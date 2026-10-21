using System.Text.Json;
using EmailWorker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

using var encerramento = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; encerramento.Cancel(); };
var factory = new ConnectionFactory
{
    Uri = new Uri(Environment.GetEnvironmentVariable("RabbitMq__Uri") ?? "amqp://guest:guest@localhost:5672/")
};
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync(Filas.EmailsFalhos, true, false, false);
await channel.QueueDeclareAsync(Filas.EmailsAtualizacao, true, false, false, Filas.ArgumentosEmail());
await channel.BasicQosAsync(0, 1, false);
var email = new EmailService();
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, entrega) =>
{
    try
    {
        var pedido = JsonSerializer.Deserialize<PedidoAtualizadoEvent>(entrega.Body.Span)
            ?? throw new JsonException("Evento de atualizacao vazio.");
        // Tenta novamente falhas temporarias de SMTP, sem confirmar antes do envio.
        for (var tentativa = 1; ; tentativa++)
        {
            try { await email.EnviarAsync(pedido); break; }
            catch (Exception ex) when (tentativa < 3 &&
                (ex is MailKit.Net.Smtp.SmtpCommandException or MailKit.Net.Smtp.SmtpProtocolException
                    or IOException or System.Net.Sockets.SocketException))
            {
                Console.Error.WriteLine($"Falha SMTP. Nova tentativa {tentativa + 1}/3 em 5 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(5), encerramento.Token);
            }
        }
        await channel.BasicAckAsync(entrega.DeliveryTag, false);
        Console.WriteLine($"E-mail aceito pelo SMTP: pedido {pedido.PedidoId}, status {pedido.StatusAtual}.");
    }
    catch (OperationCanceledException) when (encerramento.IsCancellationRequested) { }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Falha no envio ({ex.GetType().Name}). Mensagem encaminhada para {Filas.EmailsFalhos}.");
        await channel.BasicNackAsync(entrega.DeliveryTag, false, requeue: false);
    }
};
await channel.BasicConsumeAsync(Filas.EmailsAtualizacao, autoAck: false, consumer: consumer);
Console.WriteLine($"EmailWorker aguardando atualizacoes na fila {Filas.EmailsAtualizacao}. Ctrl+C para sair.");
try { await Task.Delay(Timeout.Infinite, encerramento.Token); }
catch (OperationCanceledException) when (encerramento.IsCancellationRequested) { }
