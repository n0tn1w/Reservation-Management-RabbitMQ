namespace MainService;

using Microsoft.Extensions.Configuration;
using MainService.Services.Interfaces;
using RabbitMQ.Client.Events;
using MainService.Models;
using System.Text.Json;
using RabbitMQ.Client;
using Utils;

public class OutsideReceiverWorker : BackgroundService
{
    private readonly ILogger<OutsideReceiverWorker> _logger;
    private readonly IDBManagement _dbWriter;
    private readonly IValidatorService _validatorService;
    private readonly ISenderHandledReservations _senderHandledReservations;
    private readonly IConnection _connection;
    private readonly IConfiguration _configuration;

    public OutsideReceiverWorker(ILogger<OutsideReceiverWorker> logger,
        IDBManagement dbWriter,
        IValidatorService validatorService,
        ISenderHandledReservations senderHandledReservations,
        IConnection connection,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbWriter = dbWriter;
        _validatorService = validatorService;
        _senderHandledReservations = senderHandledReservations;
        _connection = connection;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = _connection.CreateModel();

        var rabbitMQSettings = _configuration.GetSection("RabbitMQ");

        channel.ExchangeDeclare(
            exchange: rabbitMQSettings["MainExchange"],
            type: ExchangeType.Topic);

        channel.QueueDeclare(
            queue: rabbitMQSettings["MainQueue"],
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: rabbitMQSettings["MainQueue"],
            exchange: rabbitMQSettings["MainExchange"],
            routingKey: rabbitMQSettings["MainQueueRoutingKey"],
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            _logger.LogInformation("Received from Queue: " + ea.RoutingKey + " With Len: " + body.Length);
            Task.Run(() => ProcessMessage(body));
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(
            queue: rabbitMQSettings["MainQueue"],
            autoAck: false,
            consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessage(byte[] message)
    {
        try
        {
            ReservationRequest reservationRequest;

            string validationErrors = "";
            try
            {
                reservationRequest = JsonOperations.Deserialize<ReservationRequest>(message);
                validationErrors = _validatorService.ValidateReservation(reservationRequest);
            }
            catch (Exception e)
            {
                validationErrors = "Invalid Date";
            }

            var validationResult = new ReservationValidationDTO
            {
                RawRequest = message,
                DT = DateTime.Now,
                ValidationResultCode = validationErrors.Length > 0 ? 9 : 0
            };

            long inserted_id = await SaveIntoDB(validationResult);

            if (inserted_id == -1)
            {
                // Happens when the db procedure fails!
                return;
            }
            else
            {
                await _senderHandledReservations.SendForward(validationResult, inserted_id);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Invalid JSON format: {ex.Message} ");
        }
    }

    private async Task<long> SaveIntoDB(ReservationValidationDTO validationResult)
    {
        return await _dbWriter.SaveValidationResultAsync(validationResult);
    }
}   
