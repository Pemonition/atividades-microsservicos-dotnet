using System.Text.Json;
using RabbitMQ.Client;
using Shared;

namespace OrderApi.Services;

public class RabbitMqProducer : IPedidoProducer
{
    private readonly string _uri;
    public RabbitMqProducer(IConfiguration configuration)
    {
        _uri = configuration["RabbitMq:Uri"] ?? throw new InvalidOperationException("Configure RabbitMq:Uri.");
    }

    public Task PublicarPedidoAsync(PedidoCriadoEvent evento) => PublicarAsync(evento, Filas.PedidosCriados);
    public Task PublicarAtualizacaoAsync(PedidoAtualizadoEvent evento) => PublicarAsync(evento, Filas.EmailsAtualizacao);

    private async Task PublicarAsync<T>(T evento, string fila)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_uri) };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true));
        if (fila == Filas.EmailsAtualizacao)
            await channel.QueueDeclareAsync(Filas.EmailsFalhos, true, false, false);
        await channel.QueueDeclareAsync(fila, true, false, false,
            fila == Filas.EmailsAtualizacao ? Filas.ArgumentosEmail() : null);
        await channel.BasicPublishAsync(exchange: "", routingKey: fila, mandatory: true,
            basicProperties: new BasicProperties { Persistent = true, ContentType = "application/json" },
            body: JsonSerializer.SerializeToUtf8Bytes(evento));
    }
}
