namespace Event.Shared.Events;

/// <summary>
/// Evento que se dispara cuando se crea un nuevo usuario
/// </summary>
public class UserCreatedEvent : DomainEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
