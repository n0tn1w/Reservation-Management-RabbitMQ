namespace MainService;

using Microsoft.Extensions.Configuration;
using MainService.Services.Interfaces;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

public class ResponseReceiverWorker : BackgroundService
{
    private readonly ILogger<ResponseReceiverWorker> _logger;
    private readonly IDBManagement _dbWriter;
    private readonly IConnection _connection;
    private readonly IConfiguration _configuration;

    public ResponseReceiverWorker(ILogger<ResponseReceiverWorker> logger,
        IDBManagement dbWriter,
        IConnection connection,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbWriter = dbWriter;
        _connection = connection;
        _configuration = configuration;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = _connection.CreateModel();

        var rabbitMQSettings = _configuration.GetSection("RabbitMQ");

        channel.ExchangeDeclare(
            exchange: rabbitMQSettings["ResponseSuccessReservationsExchange"],
            type: ExchangeType.Direct);

        channel.QueueDeclare(
            queue: rabbitMQSettings["SuccessResponseQueue"],
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: rabbitMQSettings["SuccessResponseQueue"],
            exchange: rabbitMQSettings["ResponseSuccessReservationsExchange"],
            routingKey: rabbitMQSettings["SuccessResponseRoutingKey"],
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            _logger.LogInformation($"Received from Queue: {ea.RoutingKey} with len: {body.Length}");

            Task.Run(() => SaveSuccessResponse(body));
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(
            queue: rabbitMQSettings["SuccessResponseQueue"],
            autoAck: false,
            consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async void SaveSuccessResponse(byte[] message)
    {
        long insertedId = BitConverter.ToInt64(message);
        await _dbWriter.SaveValidationResultAsync(insertedId);
    }
}
