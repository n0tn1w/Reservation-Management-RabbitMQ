using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;

public class ReservationRequest
{
    public string ClientName { get; set; }
    public string ClientTelephone { get; set; }
    public int NumberOfReservedTable { get; set; }
    public DateTime DateOfReservation { get; set; }

    public override string ToString()
    {
        return $"Client Name: {ClientName}, " +
               $"Client Telephone: {ClientTelephone}, " +
               $"Number of Reserved Table: {NumberOfReservedTable}, " +
               $"Date of Reservation: {DateOfReservation}";
    }
}


class Program
{

    static void Main()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: "main_exchange", type: ExchangeType.Topic); // Should be direct
        channel.QueueDeclare(queue: "main_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(queue: "main_queue", exchange: "main_exchange", routingKey: "main_queue_key", arguments: null);

        ReservationRequest[] reservations = new ReservationRequest[]
        {
            new ReservationRequest
            {
                ClientName = "Tester Testerski",
                ClientTelephone = "0878878878",
                NumberOfReservedTable = 1,
                DateOfReservation = DateTime.Parse("2021-11-17 20:20:20")
            },
            new ReservationRequest
            {
                ClientName = "Alice Wonderland",
                ClientTelephone = "1234567890",
                NumberOfReservedTable = 2,
                DateOfReservation = DateTime.Parse("2021-11-18 18:30:00")
            },
            new ReservationRequest
            {
                ClientName = "Bob Builder",
                ClientTelephone = "0987654321",
                NumberOfReservedTable = 4,
                DateOfReservation = DateTime.Parse("2021-11-19 19:00:00")
            },
            new ReservationRequest
            {
                ClientName = "Charlie Brown",
                ClientTelephone = "5551234567b",
                NumberOfReservedTable = 3,
                DateOfReservation = DateTime.Parse("2021-11-20 20:45:00")
            },
            new ReservationRequest
            {
                ClientName = "Daisy Duck",
                ClientTelephone = "7778889999a",
                NumberOfReservedTable = 5,
                DateOfReservation = DateTime.Parse("2021-11-20 20:45:00")
            }
        };

        var random = new Random();

        while (true)
        {
            Console.WriteLine("Press [enter] to send a random reservation request.");
            Console.ReadLine();

            var reservation = reservations[random.Next(reservations.Length)];
            var message = JsonSerializer.Serialize(reservation);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(
                exchange: "main_exchange",
                routingKey: "main_queue_key",
                basicProperties: null,
                body: body
            );

            Console.WriteLine($"Sent: {message}");
        }
    }
}
