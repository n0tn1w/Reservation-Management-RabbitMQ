using FailedMessagesService;
using FailedMessagesService.Services;
using FailedMessagesService.Services.Interfaces;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory { HostName = "localhost" });
builder.Services.AddSingleton<IConnection>(sp => sp.GetRequiredService<IConnectionFactory>().CreateConnection());

builder.Services.AddSingleton<IDBManagement, DBManagement>();
builder.Services.AddHostedService<FailReceiverWorker>();


var host = builder.Build();
host.Run();
