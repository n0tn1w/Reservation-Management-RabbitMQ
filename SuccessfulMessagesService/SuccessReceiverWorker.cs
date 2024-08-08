namespace SuccessfulMessagesService;

using SuccessfulMessagesService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using SuccessfulMessagesService.Models;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;
using Utils;

public class SuccessReceiverWorker : BackgroundService
{
    private readonly ILogger<SuccessReceiverWorker> _logger;
    private readonly IDBManagement _dBManagement;
    private readonly IConnection _connection;
    private readonly ISenderResponseSuccessReservations _senderResponseSuccessReservations;
    private readonly IConfiguration _configuration;

    public SuccessReceiverWorker(
        IDBManagement dBManagement,
        ILogger<SuccessReceiverWorker> logger,
        IConnection connection,
        ISenderResponseSuccessReservations senderResponseSuccessReservations,
        IConfiguration configuration)
    {
        _logger = logger;
        _dBManagement = dBManagement;
        _connection = connection;
        _senderResponseSuccessReservations = senderResponseSuccessReservations;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = _connection.CreateModel();

        var rabbitMQSettings = _configuration.GetSection("RabbitMQ");

        channel.ExchangeDeclare(
            exchange: rabbitMQSettings["HandledReservationsExchange"],
            type: ExchangeType.Topic);

        channel.QueueDeclare(
            queue: rabbitMQSettings["SuccessQueue"],
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: rabbitMQSettings["SuccessQueue"],
            exchange: rabbitMQSettings["HandledReservationsExchange"],
            routingKey: rabbitMQSettings["SuccessfulHandledReservationsRoutingKey"],
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();

            Task.Run(() => ProcessMessage(body));

            channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(
            queue: rabbitMQSettings["SuccessQueue"],
            autoAck: false,
            consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessage(byte[] message)
    {
        try
        {
            var request = JsonOperations.Deserialize<ReservationValidationRequest>(message);

            await SaveIntoDB(request.RawRequest);
            await _senderResponseSuccessReservations.SendForward(BitConverter.GetBytes(request.Id));
        }
        catch (JsonException ex)
        {
            Console.WriteLine("Invalid JSON format: " + ex.Message);
        }
    }

    private async Task SaveIntoDB(byte[] message)
    {
        await _dBManagement.SaveSuccessfulMessageAsync(message);
    }
}
