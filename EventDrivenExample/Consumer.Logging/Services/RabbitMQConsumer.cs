using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using Event.Shared.Events;
using Consumer.Shared.Handlers;
using Consumer.Shared.Idempotency;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Consumer.Logging.Services;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private IModel? _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private const string QueueName = "logging-queue";
    private const string ExchangeName = "event-driven-exchange";

    public RabbitMQConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQConsumer> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();

        // Declarar exchange y queue
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: "topic",
            durable: true,
            autoDelete: false
        );

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        // Suscribirse a todos los eventos usando wildcard
        _channel.QueueBind(QueueName, ExchangeName, "events.*");

        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            await HandleMessageAsync(ea);
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumerTag: "logging-consumer",
            consumer: consumer
        );

        _logger.LogInformation("📊 Consumer de Logging iniciado");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

            var eventId = ea.BasicProperties?.MessageId ?? "";
            var body = ea.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);

            // Verificar idempotencia
            if (await idempotencyService.IsProcessedAsync(eventId))
            {
                _logger.LogWarning("⚠️  Evento duplicado ignorado: {EventId}", eventId);
                _channel!.BasicAck(ea.DeliveryTag, false);
                return;
            }

            _logger.LogInformation("📊 Procesando evento: {EventId}", eventId);

            var routingKey = ea.RoutingKey;
            await RouteAndHandleEventAsync(message, routingKey, scope.ServiceProvider);

            // Marcar como procesado en Redis
            await idempotencyService.MarkAsProcessedAsync(eventId);

            _channel!.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error procesando evento");
            _channel!.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    private async Task RouteAndHandleEventAsync(string message, string routingKey, IServiceProvider serviceProvider)
    {
        if (routingKey.Contains("usercreatedevent"))
        {
            var @event = JsonSerializer.Deserialize<UserCreatedEvent>(message);
            if (@event != null)
            {
                var handler = serviceProvider.GetRequiredService<IEventHandler<UserCreatedEvent>>();
                await handler.HandleAsync(@event);
            }
        }
        else if (routingKey.Contains("ordercreatedevent"))
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
            if (@event != null)
            {
                var handler = serviceProvider.GetRequiredService<IEventHandler<OrderCreatedEvent>>();
                await handler.HandleAsync(@event);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _channel?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
