namespace Event.Shared.Events;

/// <summary>
/// Evento que se dispara cuando se hace una factura
/// </summary>
public class InvoicingEvent : DomainEvent
{
    public string InvoiceId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
