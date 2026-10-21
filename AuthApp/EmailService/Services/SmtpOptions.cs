using MailKit.Security;
using MimeKit;

namespace EmailService.Services;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "cadastro@loja.local";
    public string FromName { get; set; } = "AuthApp";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.None;

    public bool IsValid()
    {
        var local = string.Equals(Host, "localhost", StringComparison.OrdinalIgnoreCase) || Host == "127.0.0.1";
        return !string.IsNullOrWhiteSpace(Host) && Port is > 0 and <= 65535 &&
            MailboxAddress.TryParse(From, out _) &&
            (local || Security is SecureSocketOptions.StartTls or SecureSocketOptions.SslOnConnect) &&
            (string.IsNullOrWhiteSpace(User) || (!string.IsNullOrWhiteSpace(Password) &&
                Security is SecureSocketOptions.StartTls or SecureSocketOptions.SslOnConnect));
    }
}
