using SuccessfulMessagesService.Services.Interfaces;
using SuccessfulMessagesService.Services;
using SuccessfulMessagesService;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory { HostName = "localhost" });
builder.Services.AddSingleton<IConnection>(sp => sp.GetRequiredService<IConnectionFactory>().CreateConnection());
builder.Services.AddSingleton<ISenderResponseSuccessReservations, SenderResponseSuccessReservations>();

builder.Services.AddSingleton<IDBManagement, DBManagement>();
builder.Services.AddHostedService<SuccessReceiverWorker>();

var host = builder.Build();
host.Run();
