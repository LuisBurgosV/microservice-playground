namespace Event.Shared.Events;

/// <summary>
/// Evento base que todos los eventos deben heredar
/// </summary>
public abstract class DomainEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}
