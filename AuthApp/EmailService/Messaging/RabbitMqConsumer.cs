using System.Text;
using System.Text.Json;
using EmailService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.DTOs;

namespace EmailService.Messaging;

public class RabbitMqConsumer : IHostedService
{
    private const string QueueName = Shared.Messaging.Filas.UsuariosCriados;

    private readonly EmailService.Services.EmailService _service;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumer(
        EmailService.Services.EmailService service,
        ILogger<RabbitMqConsumer> logger,
        IConfiguration configuration)
    {
        _service = service;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionUri = _configuration["RabbitMQ:Uri"]
            ?? throw new InvalidOperationException("Configure RabbitMQ:Uri com a URI do broker.");
        var factory = new ConnectionFactory { Uri = new Uri(connectionUri) };
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(Shared.Messaging.Filas.Falhas, true, false, false, cancellationToken: cancellationToken);
        await _channel.BasicQosAsync(0, 1, false, cancellationToken);
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: Shared.Messaging.Filas.Argumentos(),
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var user = JsonSerializer.Deserialize<UsuarioCriadoEventDto>(json);

                if (user is null)
                {
                    _logger.LogWarning("Mensagem inválida recebida da fila {QueueName}", QueueName);
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await _service.ProcessarBoasVindasAsync(user, cancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falha ao processar mensagem da fila {QueueName}", QueueName);
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
