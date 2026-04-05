namespace EventDrivenDesigns.Events
{
    /// <summary>
    /// Evento base de dominio
    /// </summary>
    public abstract class DomainEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string EventType => this.GetType().Name;
    }
}
