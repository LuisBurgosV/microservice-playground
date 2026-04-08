using Event.Shared.Events;

namespace Consumer.Shared.Handlers;

/// <summary>
/// Interfaz para manejar eventos de dominio
/// </summary>
public interface IEventHandler<in T> where T : DomainEvent
{
    Task HandleAsync(T @event);
}
