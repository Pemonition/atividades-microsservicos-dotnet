using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.DTOs;

namespace UserService.Messaging;

public class RabbitMqProducer
{
    private const string QueueName = Shared.Messaging.Filas.UsuariosCriados;
    private readonly IConfiguration _configuration;

    public RabbitMqProducer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublicaUsuarioCriadoAsync(
        UsuarioCriadoEventDto evento,
        CancellationToken cancellationToken = default)
    {
        var connectionUri = _configuration["RabbitMQ:Uri"]
            ?? throw new InvalidOperationException("Configure RabbitMQ:Uri com a URI do broker.");
        var factory = new ConnectionFactory { Uri = new Uri(connectionUri) };
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), cancellationToken);

        await channel.QueueDeclareAsync(Shared.Messaging.Filas.Falhas, true, false, false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: Shared.Messaging.Filas.Argumentos(),
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true, ContentType = "application/json" },
            body: body,
            cancellationToken: cancellationToken);
    }
}
