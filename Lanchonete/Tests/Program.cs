using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Lanchonete.Grpc;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
var temporary = Path.Combine(Path.GetTempPath(), "lanchonete-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporary);
var processes = new List<Process>();
var readers = new List<Task<string>>();
var kitchenPort = Porta(); var apiPort = Porta();
var passed = 0;
using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{apiPort}"), Timeout = TimeSpan.FromSeconds(5) };
using var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{kitchenPort}");
var kitchen = new Cozinha.CozinhaClient(channel);
try
{
    var server = Start("Restaurante.Server");
    var response = await AguardarCozinha();
    Check(response.Lanches.Count == 3 && response.Lanches[0].PrecoCentavos == 2250, "Dados iniciais gRPC incorretos.");
    Pass("Cozinha gRPC retorna os tres lanches e preco em centavos.");
    Start("Cardapio.Api");
    await AguardarApi();
    using (var menu = await Cardapio())
    {
        Check(menu.RootElement.GetProperty("quantidade").GetInt32() == 3, "Cardapio incompleto.");
        Check(menu.RootElement.GetProperty("itens")[0].GetProperty("preco").GetDecimal() == 22.50m, "Preco HTTP incorreto.");
        Check(!menu.RootElement.GetProperty("itens")[2].GetProperty("disponivel").GetBoolean(), "Item esgotado deveria estar indisponivel.");
    }
    Pass("Vitrine HTTP consulta a cozinha e apresenta preco, estoque e disponibilidade.");
    using (var disponiveis = await Cardapio("?somenteDisponiveis=true"))
        Check(disponiveis.RootElement.GetProperty("quantidade").GetInt32() == 2, "Filtro incorreto.");
    Pass("Filtro retorna apenas itens com estoque.");

    await kitchen.AtualizarEstoqueAsync(new AtualizarEstoqueRequest { LancheId = 1, Quantidade = 0 });
    using (var disponiveis = await Cardapio("?somenteDisponiveis=true"))
        Check(disponiveis.RootElement.GetProperty("quantidade").GetInt32() == 1, "Vitrine nao refletiu atualizacao.");
    Pass("Atualizacao de estoque na cozinha aparece na vitrine pela proxima chamada gRPC.");
    await EsperarErro(StatusCode.InvalidArgument, async () =>
        { await kitchen.AtualizarEstoqueAsync(new AtualizarEstoqueRequest { LancheId = 1, Quantidade = -1 }); });
    Pass("Estoque negativo e rejeitado com InvalidArgument.");
    await EsperarErro(StatusCode.NotFound, async () =>
        { await kitchen.AtualizarEstoqueAsync(new AtualizarEstoqueRequest { LancheId = 999, Quantidade = 1 }); });
    Pass("Lanche inexistente retorna NotFound.");

    await Stop(server);
    server = Start("Restaurante.Server");
    response = await AguardarCozinha();
    Check(response.Lanches.Single(l => l.Id == 1).Estoque == 0, "Estoque nao persistiu.");
    Pass("SQLite preserva estoque apos reiniciar o servidor da cozinha.");
    await Stop(server);
    var failure = await http.GetAsync("/api/cardapio");
    Check(failure.StatusCode == HttpStatusCode.ServiceUnavailable, $"Esperado HTTP 503, recebido {(int)failure.StatusCode}.");
    Check((await http.GetAsync("/health")).StatusCode == HttpStatusCode.OK, "Processo da vitrine deveria continuar ativo.");
    Pass("Cozinha fora do ar retorna 503; vitrine permanece ativa.");
    Console.WriteLine($"Resultado: {passed}/8 cenarios aprovados.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FALHOU: {ex}");
    return 1;
}
finally
{
    foreach (var process in processes) await Stop(process);
    foreach (var reader in readers) await reader;
    var cleanupPath = Path.GetFullPath(temporary);
    var expectedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
    if (!cleanupPath.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) ||
        !Path.GetFileName(cleanupPath).StartsWith("lanchonete-", StringComparison.Ordinal))
        throw new InvalidOperationException("Caminho temporario de limpeza invalido.");
    Directory.Delete(cleanupPath, recursive: true);
}

Process Start(string name)
{
    var folder = Path.Combine(root, name);
    var dll = Path.Combine(folder, "bin/Debug/net10.0", name + ".dll");
    if (!File.Exists(dll)) throw new FileNotFoundException("Compile a solucao antes de executar os testes.", dll);
    var info = new ProcessStartInfo("dotnet") { WorkingDirectory = folder, UseShellExecute = false,
        CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    info.ArgumentList.Add(dll);
    info.Environment["Grpc__Port"] = kitchenPort.ToString();
    info.Environment["Database__Path"] = Path.Combine(temporary, "estoque.db");
    info.Environment["Cozinha__Url"] = $"http://127.0.0.1:{kitchenPort}";
    info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    if (name == "Cardapio.Api")
    { info.ArgumentList.Add("--urls"); info.ArgumentList.Add($"http://127.0.0.1:{apiPort}"); }
    var process = Process.Start(info)!;
    processes.Add(process);
    readers.Add(process.StandardOutput.ReadToEndAsync()); readers.Add(process.StandardError.ReadToEndAsync());
    return process;
}
async Task<ListarLanchesResponse> AguardarCozinha()
{
    for (var i = 0; i < 60; i++)
    {
        try { return await kitchen.ListarLanchesAsync(new ListarLanchesRequest(), deadline: DateTime.UtcNow.AddSeconds(1)); }
        catch (RpcException) { await Task.Delay(200); }
    }
    throw new Exception("Cozinha nao iniciou.");
}
async Task AguardarApi()
{
    for (var i = 0; i < 60; i++)
    {
        try { if ((await http.GetAsync("/health")).IsSuccessStatusCode) return; }
        catch (HttpRequestException) { }
        await Task.Delay(200);
    }
    throw new Exception("Vitrine nao iniciou.");
}
async Task<JsonDocument> Cardapio(string query = "")
{
    var response = await http.GetAsync("/api/cardapio" + query);
    response.EnsureSuccessStatusCode();
    return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
void Pass(string message) { passed++; Console.WriteLine("PASSOU: " + message); }
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static async Task EsperarErro(StatusCode code, Func<Task> call)
{
    try { await call(); }
    catch (RpcException ex) when (ex.StatusCode == code) { return; }
    throw new Exception("Esperado status gRPC " + code);
}
static async Task Stop(Process process)
{
    if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
}
static int Porta()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port;
}
