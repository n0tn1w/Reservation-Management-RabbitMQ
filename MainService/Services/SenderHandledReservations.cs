namespace MainService.Services;

using Microsoft.Extensions.Configuration;
using MainService.Services.Interfaces;
using System.Collections.Concurrent;
using MainService.Models;
using RabbitMQ.Client;
using Utils;

public class SenderHandledReservations : ISenderHandledReservations
{
    private readonly IConnection _connection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SenderHandledReservations> _logger;
    private readonly ConcurrentDictionary<ulong, ReservationValidationRequest> _outstandingConfirms;

    public SenderHandledReservations(IConnection connection,
        IConfiguration configuration,
        ILogger<SenderHandledReservations> logger)
    {
        _connection = connection;
        _configuration = configuration;
        _logger = logger;
        _outstandingConfirms = new ConcurrentDictionary<ulong, ReservationValidationRequest>();
    }

    public async Task SendForward(ReservationValidationDTO validationResult, long inserted_id)
    {
        using var channel = _connection.CreateModel();

        channel.ConfirmSelect();
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

        channel.QueueDeclare(
            queue: rabbitMQSettings["FailQueue"],
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: rabbitMQSettings["SuccessQueue"],
            exchange: rabbitMQSettings["HandledReservationsExchange"],
            routingKey: rabbitMQSettings["SuccessfulHandledReservationsRoutingKey"],
            arguments: null);

        channel.QueueBind(
            queue: rabbitMQSettings["FailQueue"],
            exchange: rabbitMQSettings["HandledReservationsExchange"],
            routingKey: rabbitMQSettings["FailedHandledReservationsRoutingKey"],
            arguments: null);

        string routingKey = validationResult.ValidationResultCode == 0
            ? rabbitMQSettings["SuccessfulHandledReservationsRoutingKey"]
            : rabbitMQSettings["FailedHandledReservationsRoutingKey"];

        var request = new ReservationValidationRequest
        {
            Id = inserted_id,
            RawRequest = validationResult.RawRequest
        };

        var body = JsonOperations.Serialize(request);
        var sequenceNumber = channel.NextPublishSeqNo;
        _outstandingConfirms.TryAdd(sequenceNumber, request);

        channel.BasicPublish(
            exchange: rabbitMQSettings["HandledReservationsExchange"],
            routingKey: routingKey,
            basicProperties: null,
            body: body);

        _logger.LogInformation($"Sent to Queue: {routingKey} with Id: {inserted_id}");

        channel.BasicAcks += (sender, ea) =>
            HandleAck(ea.DeliveryTag, ea.Multiple);

        channel.BasicNacks += (sender, ea) =>
            HandleNack(ea.DeliveryTag, ea.Multiple);

        await WaitForConfirmsAsync(channel);

        channel.Close();
        await Task.CompletedTask;
    }

    private void HandleAck(ulong deliveryTag, bool multiple)
    {
        CleanOutstandingConfirms(deliveryTag, multiple);
        _logger.LogInformation($"Message with DeliveryTag {deliveryTag} has been acknowledged. Multiple: {multiple}");
    }

    private void HandleNack(ulong deliveryTag, bool multiple)
    {
        _outstandingConfirms.TryGetValue(deliveryTag, out var failedRequest);
        _logger.LogWarning($"Message with DeliveryTag {deliveryTag} has been nack-ed. Multiple: {multiple}");

        if (failedRequest != null)
        {
            _logger.LogWarning($"Failed id: {failedRequest.Id}, RawRequest: {failedRequest.RawRequest}");
        }

        CleanOutstandingConfirms(deliveryTag, multiple);
    }

    private void CleanOutstandingConfirms(ulong sequenceNumber, bool multiple)
    {
        if (multiple)
        {
            var confirmed = _outstandingConfirms.Where(k => k.Key <= sequenceNumber);
            foreach (var entry in confirmed)
            {
                _outstandingConfirms.TryRemove(entry.Key, out _);
            }
        }
        else
        {
            _outstandingConfirms.TryRemove(sequenceNumber, out _);
        }
    }

    private async Task WaitForConfirmsAsync(IModel channel)
    {
        while (true)
        {
            if (channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
            {
                break;
            }

            _logger.LogWarning("Waiting for message confirms...");
            await Task.Delay(100);
        }
    }
}
