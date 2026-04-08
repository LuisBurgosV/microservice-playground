namespace Event.Shared.Events;

/// <summary>
/// Evento que se dispara cuando se realiza un pedido
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
