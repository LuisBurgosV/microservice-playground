using EventDrivenDesigns.Events;

namespace EventDrivenDesigns.Bus
{
    /// <summary>
    /// Interfaz del manejador de eventos
    /// Cada handler implementa esto para procesar un tipo específico de evento
    /// </summary>
    public interface IEventHandler<TEvent> where TEvent : DomainEvent
    {
        Task HandleAsync(TEvent @event);
    }
}
