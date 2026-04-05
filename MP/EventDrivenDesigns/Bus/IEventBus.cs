using EventDrivenDesigns.Events;

namespace EventDrivenDesigns.Bus
{
    /// <summary>
    /// Event Bus Interface
    /// Permite publicar eventos y que múltiples handlers se suscriban
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Suscribir un handler a un tipo de evento
        /// </summary>
        void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent;

        /// <summary>
        /// Publicar un evento (notifica a todos los handlers suscritos)
        /// </summary>
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
    }
}
