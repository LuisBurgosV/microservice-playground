using Producer;
using Producer.Services;
using Event.Shared.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEventProducer();

var host = builder.Build();

// Simular la publicación de eventos
var publisher = host.Services.GetRequiredService<IEventPublisher>();

Console.WriteLine("🚀 Iniciando Producer - Event Driven Architecture\n");

// Publicar eventos de usuarios
await PublishUserEvents(publisher);

// Publicar eventos de pedidos
await PublishOrderEvents(publisher);

Console.WriteLine("\n✅ Todos los eventos han sido publicados. Presiona cualquier tecla para salir...");
Console.ReadKey();

static async Task PublishUserEvents(IEventPublisher publisher)
{
    Console.WriteLine("📤 Publicando eventos de usuarios...\n");

    var users = new[]
    {
        new { Id = "user-001", Email = "juan@example.com", Name = "Juan Pérez" },
        new { Id = "user-002", Email = "maria@example.com", Name = "María García" },
        new { Id = "user-003", Email = "carlos@example.com", Name = "Carlos López" }
    };

    foreach (var user in users)
    {
        var evt = new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.Name
        };

        await publisher.PublishAsync(evt);
        await Task.Delay(500);
    }
}

static async Task PublishOrderEvents(IEventPublisher publisher)
{
    Console.WriteLine("\n📤 Publicando eventos de pedidos...\n");

    var orders = new[]
    {
        new { Id = "order-001", UserId = "user-001", Amount = 99.99m, Desc = "Laptop" },
        new { Id = "order-002", UserId = "user-002", Amount = 49.99m, Desc = "Mouse" },
        new { Id = "order-003", UserId = "user-003", Amount = 299.99m, Desc = "Monitor" }
    };

    foreach (var order in orders)
    {
        var evt = new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Amount = order.Amount,
            Description = order.Desc
        };

        await publisher.PublishAsync(evt);
        await Task.Delay(500);
    }
}
