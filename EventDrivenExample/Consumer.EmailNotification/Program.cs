using Consumer.EmailNotification;
using Consumer.EmailNotification.Handlers;
using Consumer.EmailNotification.Services;
using Consumer.Shared.Handlers;
using Consumer.Shared.Idempotency;
using Event.Shared.Events;
using RabbitMQ.Client;
using StackExchange.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configurar RabbitMQ
var rabbitFactory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    DispatchConsumersAsync = true
};

var connection = rabbitFactory.CreateConnection();
builder.Services.AddSingleton(connection);

// Configurar Redis
try
{
    var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");
    builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
}
catch (Exception ex)
{
    Console.WriteLine($"Error conectando a Redis: {ex.Message}");
    throw;
}

builder.Services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

// Registrar handlers de eventos
builder.Services.AddScoped<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
builder.Services.AddScoped<IEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();

// Registrar consumer
builder.Services.AddHostedService<RabbitMQConsumer>();

builder.Logging.AddConsole();

var host = builder.Build();

Console.WriteLine("🚀 Consumer.EmailNotification - Event Driven Architecture");
Console.WriteLine("Esperando eventos...\n");

await host.RunAsync();
