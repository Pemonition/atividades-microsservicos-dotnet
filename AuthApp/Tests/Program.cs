using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using System.Text;
using EmailService.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.DTOs;
using UserService.DTOs;
using Processor = EmailService.Services.EmailService;

var tests = new (string Name, Func<Task> Run)[]
{
    ("Conteudo SMTP inclui destinatario, nome, identificador e data", async () =>
    {
        await using var server = new SmtpStub();
        var user = Usuario();
        await new Processor(Options.Create(new SmtpOptions { Port = server.Port }))
            .ProcessarBoasVindasAsync(user);
        var raw = await server.Message;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(raw));
        using var message = MimeMessage.Load(stream);
        Check(message.To.Mailboxes.Single().Address == user.Email, "Destinatario incorreto");
        Check(message.From.Mailboxes.Single().Address == "cadastro@loja.local", "Remetente incorreto");
        Check(message.Subject == "Boas-vindas!", "Assunto incorreto");
        var text = message.TextBody ?? throw new Exception("Corpo de texto ausente");
        Check(text.Contains(user.Name) && text.Contains(user.Id.ToString()) &&
            text.Contains(user.DataCriacao.ToString("O")), "Dados ausentes no corpo");
    }),
    ("Recusa do SMTP propaga erro ao chamador", async () =>
    {
        await using var server = new SmtpStub(reject: true);
        await Throws<SmtpCommandException>(() => new Processor(Options.Create(new SmtpOptions { Port = server.Port }))
            .ProcessarBoasVindasAsync(Usuario()));
    }),
    ("Email invalido e rejeitado antes da rede", async () =>
    {
        var user = Usuario(); user.Email = "invalido";
        await Throws<ValidationException>(() => new Processor(Options.Create(new SmtpOptions()))
            .ProcessarBoasVindasAsync(user));
    }),
    ("Id vazio e rejeitado antes da rede", async () =>
    {
        var user = Usuario(); user.Id = Guid.Empty;
        await Throws<ValidationException>(() => new Processor(Options.Create(new SmtpOptions()))
            .ProcessarBoasVindasAsync(user));
    }),
    ("Cadastro exige nome e email validos", () =>
    {
        var request = new CriarUsuarioRequestDto { Nome = "", Email = "invalido" };
        Check(!Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true), "DTO invalido aceito");
        return Task.CompletedTask;
    }),
    ("Gmail com TLS e senha de aplicativo e configuracao valida", () =>
    {
        Check(new SmtpOptions { Host = "smtp.gmail.com", Port = 587, From = "aluno@gmail.com",
            User = "aluno@gmail.com", Password = "somente-teste", Security = SecureSocketOptions.StartTls }.IsValid(), "Gmail invalido");
        return Task.CompletedTask;
    }),
    ("SMTP externo sem TLS e credenciais sem senha sao rejeitados", () =>
    {
        Check(!new SmtpOptions { Host = "smtp.gmail.com" }.IsValid(), "SMTP externo sem TLS aceito");
        Check(!new SmtpOptions { User = "aluno", Security = SecureSocketOptions.StartTls }.IsValid(), "Senha vazia aceita");
        Check(!new SmtpOptions { User = "aluno", Password = "teste" }.IsValid(), "Login sem TLS aceito");
        return Task.CompletedTask;
    })
};
var failures = 0;
foreach (var test in tests)
{
    try { await test.Run(); Console.WriteLine($"PASSOU: {test.Name}"); }
    catch (Exception ex) { failures++; Console.Error.WriteLine($"FALHOU: {test.Name}: {ex.Message}"); }
}
Console.WriteLine($"Resultado: {tests.Length - failures}/{tests.Length} testes aprovados.");
return failures == 0 ? 0 : 1;

static UsuarioCriadoEventDto Usuario() => new()
{
    Id = Guid.NewGuid(), Name = "João da Silva", Email = "aluno@example.com",
    DataCriacao = new DateTime(2026, 10, 21, 20, 0, 0, DateTimeKind.Utc)
};
static void Check(bool condition, string error) { if (!condition) throw new Exception(error); }
static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new Exception($"Esperado {typeof(T).Name}");
}

// Servidor SMTP de teste: nenhum email e encaminhado para a internet.
sealed class SmtpStub : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _timeout = new(TimeSpan.FromSeconds(15));
    private readonly Task _server;
    private readonly TaskCompletionSource<string> _message = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Port { get; }
    public Task<string> Message => _message.Task.WaitAsync(_timeout.Token);

    public SmtpStub(bool reject = false)
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _server = Serve(reject);
    }

    private async Task Serve(bool reject)
    {
        using var client = await _listener.AcceptTcpClientAsync(_timeout.Token);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };
        await writer.WriteLineAsync("220 localhost SMTP test");
        while (await reader.ReadLineAsync(_timeout.Token) is { } line)
        {
            if (line.StartsWith("QUIT")) { await writer.WriteLineAsync("221 Bye"); break; }
            if (line.StartsWith("DATA"))
            {
                await writer.WriteLineAsync("354 Send data");
                var body = new StringBuilder();
                while (await reader.ReadLineAsync(_timeout.Token) is { } data && data != ".")
                    body.AppendLine(data.StartsWith("..") ? data[1..] : data);
                _message.TrySetResult(body.ToString());
                await writer.WriteLineAsync(reject ? "550 Rejected for test" : "250 Accepted");
            }
            else await writer.WriteLineAsync("250 localhost");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _timeout.Cancel(); _listener.Stop();
        try { await _server; } catch (OperationCanceledException) { } catch (IOException) { }
        _timeout.Dispose();
    }
}
