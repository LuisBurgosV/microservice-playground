using Consumer.Invoicing.Handlers;
using Consumer.Invoicing.Services;
using Consumer.Shared.Handlers;
using Consumer.Shared.Idempotency;
using Event.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Configurar RabbitMQ
var rabbitFactory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    ConsumerDispatchConcurrency = 4
};

var connection = await rabbitFactory.CreateConnectionAsync();
builder.Services.AddSingleton(connection);

// Configurar Redis
try
{
    var reddisConnection = ConnectionMultiplexer.Connect("localhost:6379");
    builder.Services.AddSingleton<IConnectionMultiplexer>(reddisConnection);
}
catch (Exception ex)
{
    Console.WriteLine($"Error conectando a Redis: {ex.Message}");
    throw;
}

builder.Services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

// Registrar handlers de evenos
builder.Services.AddScoped<IEventHandler<InvoicingEvent>, InvoicingEventHandler>();

// Registrar consumer
builder.Services.AddHostedService<RabbitMQConsumer>();

builder.Logging.AddConsole();

var host = builder.Build();

Console.WriteLine("🚀 Consumer.Invoicing - Event Driven Architecture");
Console.WriteLine("Esperando eventos...\n");

await host.RunAsync();


