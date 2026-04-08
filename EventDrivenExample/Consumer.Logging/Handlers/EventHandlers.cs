using Consumer.Shared.Handlers;
using Event.Shared.Events;
using Microsoft.Extensions.Logging;

namespace Consumer.Logging.Handlers;

public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event)
    {
        _logger.LogInformation(
            "📝 Registrando evento de usuario creado - ID: {UserId}, Email: {Email}, Nombre: {FullName}",
            @event.UserId,
            @event.Email,
            @event.FullName
        );

        // Simular almacenamiento en base de datos
        await Task.Delay(500);

        _logger.LogInformation(
            "✅ Evento de usuario registrado en base de datos"
        );
    }
}

public class OrderCreatedEventHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        _logger.LogInformation(
            "📝 Registrando evento de pedido creado - Pedido: {OrderId}, Usuario: {UserId}, Monto: ${Amount}",
            @event.OrderId,
            @event.UserId,
            @event.Amount
        );

        // Simular almacenamiento en base de datos
        await Task.Delay(500);

        _logger.LogInformation(
            "✅ Evento de pedido registrado en base de datos"
        );
    }
}
