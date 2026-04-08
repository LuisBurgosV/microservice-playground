using RabbitMQ.Client;
using Event.Shared.Events;
using System.Text.Json;

namespace Producer.Services;

/// <summary>
/// Publicador de eventos a RabbitMQ
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event) where T : DomainEvent;
}

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly string _exchangeName = "event-driven-exchange";
    private readonly string _exchangeType = "topic";

    public RabbitMQEventPublisher(IConnection connection)
    {
        _connection = connection;
        InitializeExchange();
    }

    private void InitializeExchange()
    {
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: _exchangeType,
            durable: true,
            autoDelete: false
        );
    }

    public async Task PublishAsync<T>(T @event) where T : DomainEvent
    {
        using var channel = _connection.CreateModel();

        var eventType = @event.EventType;
        var routingKey = $"events.{eventType.ToLower()}";
        var message = JsonSerializer.Serialize(@event);
        var body = System.Text.Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = @event.EventId;

        channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body
        );

        Console.WriteLine($"✉️  Evento publicado: {eventType} (ID: {@event.EventId})");
        await Task.CompletedTask;
    }
}
