using System.Globalization;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Shared;

namespace EmailWorker;

public sealed class EmailService
{
    public async Task EnviarAsync(PedidoAtualizadoEvent pedido)
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        using var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(Environment.GetEnvironmentVariable("Smtp__From") ?? "pedidos@loja.local"));
        mensagem.To.Add(MailboxAddress.Parse(pedido.ClienteEmail));
        mensagem.Subject = $"Atualização do pedido {pedido.PedidoId}";
        mensagem.Body = new TextPart("plain") { Text = $"""
            Olá, {pedido.ClienteNome}!

            Seu pedido foi atualizado.
            Pedido: {pedido.PedidoId}
            Status anterior: {pedido.StatusAnterior}
            Novo status: {pedido.StatusAtual}
            Valor total: {pedido.ValorTotal.ToString("C", cultura)}
            Data da atualização (UTC): {pedido.DataAtualizacao:dd/MM/yyyy HH:mm:ss}
            Evento: {pedido.EventoId}
            """ };

        using var smtp = new SmtpClient { Timeout = 15000 };
        var seguranca = Enum.Parse<SecureSocketOptions>(
            Environment.GetEnvironmentVariable("Smtp__Security") ?? "None", ignoreCase: true);
        await smtp.ConnectAsync(Environment.GetEnvironmentVariable("Smtp__Host") ?? "localhost",
            int.Parse(Environment.GetEnvironmentVariable("Smtp__Port") ?? "1025"), seguranca);
        var usuario = Environment.GetEnvironmentVariable("Smtp__User");
        if (!string.IsNullOrWhiteSpace(usuario))
            await smtp.AuthenticateAsync(usuario, Environment.GetEnvironmentVariable("Smtp__Password")
                ?? throw new InvalidOperationException("Configure Smtp__Password para autenticar."));

        await smtp.SendAsync(mensagem);
        await smtp.DisconnectAsync(true);
    }
}
