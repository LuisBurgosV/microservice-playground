using Consumer.Shared.Handlers;
using Event.Shared.Events;
using Microsoft.Extensions.Logging;

namespace Consumer.EmailNotification.Handlers;

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
            "📧 Enviando email de bienvenida a {Email} para el usuario {UserId}",
            @event.Email,
            @event.UserId
        );

        // Simular envío de email
        await Task.Delay(1000);

        _logger.LogInformation(
            "✅ Email de bienvenida enviado a {Email}",
            @event.Email
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
            "📧 Enviando confirmación de pedido al usuario {UserId} - Pedido: {OrderId}",
            @event.UserId,
            @event.OrderId
        );

        // Simular envío de email
        await Task.Delay(800);

        _logger.LogInformation(
            "✅ Confirmación de pedido enviada - Monto: ${Amount}",
            @event.Amount
        );
    }
}
