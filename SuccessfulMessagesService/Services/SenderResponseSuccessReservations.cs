namespace SuccessfulMessagesService.Services;

using SuccessfulMessagesService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

public class SenderResponseSuccessReservations : ISenderResponseSuccessReservations
{
    private readonly IConnection _connection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SenderResponseSuccessReservations> _logger;
    private readonly ConcurrentDictionary<ulong, byte[]> _outstandingConfirms;

    public SenderResponseSuccessReservations(IConnection connection, IConfiguration configuration, ILogger<SenderResponseSuccessReservations> logger)
    {
        _connection = connection;
        _configuration = configuration;
        _logger = logger;
        _outstandingConfirms = new ConcurrentDictionary<ulong, byte[]>();
    }

    public async Task SendForward(byte[] message)
    {
        using var channel = _connection.CreateModel();
        channel.ConfirmSelect();

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
            routingKey: rabbitMQSettings["ResponseSuccessRoutingKey"],
            arguments: null);

        var sequenceNumber = channel.NextPublishSeqNo;

        _outstandingConfirms.TryAdd(sequenceNumber, message);
        channel.BasicPublish(
            exchange: rabbitMQSettings["ResponseSuccessReservationsExchange"],
            routingKey: rabbitMQSettings["ResponseSuccessRoutingKey"],
            basicProperties: null,
            body: message);

        Console.WriteLine($"Sent message to Queue with routing key: {rabbitMQSettings["ResponseSuccessRoutingKey"]}");

        channel.BasicAcks += (sender, ea) => HandleAck(ea.DeliveryTag, ea.Multiple);
        channel.BasicNacks += (sender, ea) => HandleNack(ea.DeliveryTag, ea.Multiple);

        await WaitForConfirmsAsync(channel);

        channel.Close();
        await Task.CompletedTask;
    }

    private void HandleAck(ulong deliveryTag, bool multiple)
    {
        CleanOutstandingConfirms(deliveryTag, multiple);
        Console.WriteLine($"Message with DeliveryTag {deliveryTag} has been acknowledged. Multiple: {multiple}");
    }

    private void HandleNack(ulong deliveryTag, bool multiple)
    {
        _outstandingConfirms.TryGetValue(deliveryTag, out var failedMessage);
        Console.WriteLine($"Message with DeliveryTag {deliveryTag} has been nack-ed. Multiple: {multiple}");

        if (failedMessage != null)
        {
            Console.WriteLine($"Failed message: {failedMessage}");
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

            await Task.Delay(100);
        }
    }
}
