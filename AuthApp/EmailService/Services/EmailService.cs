using System.ComponentModel.DataAnnotations;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.DTOs;

namespace EmailService.Services;

public sealed class EmailService(IOptions<SmtpOptions> options)
{
    public async Task ProcessarBoasVindasAsync(UsuarioCriadoEventDto usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        Validator.ValidateObject(usuario, new ValidationContext(usuario), validateAllProperties: true);
        if (usuario.Id == Guid.Empty || usuario.DataCriacao == default)
            throw new ValidationException("Id e DataCriacao sao obrigatorios.");
        if (string.IsNullOrWhiteSpace(usuario.Name) || !MailboxAddress.TryParse(usuario.Email, out var destinatario))
            throw new ArgumentException("Informe nome e email valido.");
        var config = options.Value;
        using var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(config.FromName, config.From));
        mensagem.To.Add(destinatario);
        mensagem.Subject = "Boas-vindas!";
        mensagem.Body = new TextPart("plain") { Text = $"Olá, {usuario.Name}!\n\nSeu cadastro foi recebido com sucesso.\nIdentificador: {usuario.Id}\nData (UTC): {usuario.DataCriacao:O}\n\nSeja bem-vindo!" };
        using var smtp = new SmtpClient { Timeout = 15000 };
        await smtp.ConnectAsync(config.Host, config.Port, config.Security, cancellationToken);
        if (!string.IsNullOrWhiteSpace(config.User))
            await smtp.AuthenticateAsync(config.User, config.Password, cancellationToken);
        await smtp.SendAsync(mensagem, cancellationToken);
        // Falha apenas no QUIT nao deve repetir um email ja aceito pelo servidor.
        try { await smtp.DisconnectAsync(true, cancellationToken); }
        catch (IOException) { }
        catch (MailKit.Net.Smtp.SmtpProtocolException) { }
    }
}
