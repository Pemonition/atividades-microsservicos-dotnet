using Grpc.Core;
using Grpc.Net.Client;
using Lanchonete.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(_ => GrpcChannel.ForAddress(builder.Configuration["Cozinha:Url"] ?? "http://127.0.0.1:5301"));
builder.Services.AddSingleton(sp => new Cozinha.CozinhaClient(sp.GetRequiredService<GrpcChannel>()));
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapGet("/health", () => Results.Ok(new { status = "ok", servico = "Cardapio.Api" }));
app.MapGet("/api/cardapio", async (bool? somenteDisponiveis, Cozinha.CozinhaClient cozinha, CancellationToken ct) =>
{
    try
    {
        var response = await cozinha.ListarLanchesAsync(new ListarLanchesRequest(),
            deadline: DateTime.UtcNow.AddSeconds(3), cancellationToken: ct);
        var itens = response.Lanches.Where(l => somenteDisponiveis != true || l.Estoque > 0)
            .Select(l => new { l.Id, l.Nome, preco = l.PrecoCentavos / 100m, l.PrecoCentavos, l.Estoque,
                disponivel = l.Estoque > 0 }).ToArray();
        return Results.Ok(new { quantidade = itens.Length, itens });
    }
    catch (RpcException ex) when (!ct.IsCancellationRequested)
    {
        var status = ex.StatusCode == StatusCode.Unavailable ? 503 : ex.StatusCode == StatusCode.DeadlineExceeded ? 504 : 502;
        return Results.Problem(statusCode: status, title: "Nao foi possivel consultar a cozinha.");
    }
});
app.Run();
