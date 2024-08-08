using MainService;
using MainService.Services.Interfaces;
using MainService.Services;
using Serilog;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory { HostName = "localhost" });
builder.Services.AddSingleton<IConnection>(sp => sp.GetRequiredService<IConnectionFactory>().CreateConnection());
builder.Services.AddSingleton<ISenderHandledReservations, SenderHandledReservations>();
builder.Services.AddSingleton<IValidatorService, ValidatorService>();


builder.Services.AddHostedService<ResponseReceiverWorker>();
builder.Services.AddHostedService<OutsideReceiverWorker>();

builder.Services.AddSingleton<IDBManagement, DBManagement>();

var host = builder.Build();
host.Run();
