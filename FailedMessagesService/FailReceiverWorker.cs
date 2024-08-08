namespace FailedMessagesService;

using FailedMessagesService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using FailedMessagesService.Models;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Utils;

public class FailReceiverWorker : BackgroundService
{

    private readonly ILogger<FailReceiverWorker> _logger;
    private readonly IDBManagement _dBManagement;
    private readonly IConnection _connection;
    private readonly IConfiguration _configuration;

    public FailReceiverWorker(IDBManagement dBManagement,
         ILogger<FailReceiverWorker> logger,
         IConnection connection,
         IConfiguration configuration)
    {
        _logger = logger;
        _dBManagement = dBManagement;
        _connection = connection;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = _connection.CreateModel();

        var rabbitMQSettings = _configuration.GetSection("RabbitMQ");

        channel.ExchangeDeclare(
            exchange: rabbitMQSettings["HandledReservationsExchange"],
            type: ExchangeType.Topic);

        channel.QueueDeclare(queue: rabbitMQSettings["FailQueue"],
            durable: false, exclusive: false, autoDelete: false, arguments: null);

        channel.QueueBind(
            queue: rabbitMQSettings["FailQueue"],
            exchange: rabbitMQSettings["HandledReservationsExchange"],
            routingKey: rabbitMQSettings["FailedHandledReservationsRoutingKey"],
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();

            Task.Run(() => SaveIntoDB(body));

            channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(queue: rabbitMQSettings["FailQueue"],
                 autoAck: false,
                 consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task SaveIntoDB(byte[] message)
    {
        var request = JsonOperations.Deserialize<ReservationValidationRequest>(message);
        await _dBManagement.SaveFailedMessageAsync(request.RawRequest);
    }
}
