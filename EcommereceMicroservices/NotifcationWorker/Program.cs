// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.Json;
using System.Xml.Schema;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

Console.Title = "Micrsosservico de Notificacao";
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== MICROSSERVICO INICIADO ===");
Console.ResetColor();

var rabbitMqUri = Environment.GetEnvironmentVariable("RabbitMq__Uri") ?? "amqp://guest:guest@localhost:5672/";

var factory = new ConnectionFactory
{
    Uri = new Uri(rabbitMqUri)
};

Console.WriteLine("Conectando ao RabbitMq na nuvem....");

using var connection = await factory.CreateConnectionAsync();
using var chanel = await connection.CreateChannelAsync();


await chanel.QueueDeclareAsync(
    queue: "pedidos-queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Conectado com sucesso!! Aguaradando pedidos na fila 'pedidos-queue'");

var consumer = new AsyncEventingBasicConsumer(chanel);

consumer.ReceivedAsync += async (XmlSchemaContentModel, ea) =>
{
    try
    {
        var bytes = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(bytes);
        var pedido = JsonSerializer.Deserialize<PedidoCriadoEvent>(json);

        if (pedido is not null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("/n---------------------------------------------------------------");
            Console.WriteLine($"[NOVO PEDIDO CHEGOU] Id: {pedido.PedidoId}");
            Console.ResetColor();
            Console.WriteLine($"Cliente : {pedido.ClienteNome}");
            Console.WriteLine($"E-mail  : {pedido.ClienteEmail}");
            Console.WriteLine($"Total   : {pedido.ValorTotal}");
            Console.WriteLine($"Criado  : {pedido.DataCriacao:dd/MM/yyyy HH:mm:ss} UTC");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Simulacao: Enviando email de confirmacao para {pedido.ClienteEmail}");
            Console.ResetColor();
            Console.WriteLine("/n---------------------------------------------------------------");

        }

        await chanel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);

    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERRO AO PROCESSAR]: {e.Message}");

    }
};
await chanel.BasicConsumeAsync(
    queue: "pedidos-queue",
    autoAck: false,
    consumer: consumer);



Console.WriteLine("...");
await Task.Delay(Timeout.Infinite);
