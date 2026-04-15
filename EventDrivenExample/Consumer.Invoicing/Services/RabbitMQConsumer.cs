using Consumer.Shared.Handlers;
using Consumer.Shared.Idempotency;
using Event.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Consumer.Invoicing.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private IChannel? _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private const string QueueName = "invoicing-queue";
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
            _channel = await _connection.CreateChannelAsync();

            // Declarar exchange y queue
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );
            
            await _channel.QueueDeclareAsync(
                queue: QueueName, 
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Suscribirse a eventos de facturación
            await _channel.QueueBindAsync(
                queue: QueueName, 
                exchange: ExchangeName, 
                routingKey: "events.invoicingevent"
            );

            await _channel.BasicQosAsync(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                await HandleMessageAsync(ea);
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumerTag: "invoicing-consumer",
                consumer: consumer
            );

            _logger.LogInformation("📊 Consumer de Invoicing iniciado");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

                var eventId = ea.BasicProperties.MessageId ?? "";
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // Veificar idempotencia
                if (await idempotencyService.IsProcessedAsync(eventId))
                {
                    _logger.LogWarning("⚠️  Evento duplicado ignorado: {EventId}", eventId);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                _logger.LogInformation("📨 Procesando evento: {EventId}", eventId);

                var routingKey = ea.RoutingKey;
                await RouteAndHandleEventAsync(message, routingKey, scope.ServiceProvider);

                // Marcar como procesado en Redis
                await idempotencyService.MarkAsProcessedAsync(eventId);
                    
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando evento");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }

        private async Task RouteAndHandleEventAsync(string message, string routingKey, IServiceProvider serviceProvider)
        {
            if (routingKey.Contains("invoicingevent"))
            {
                var @event = JsonSerializer.Deserialize<InvoicingEvent>(message);
                if (@event != null)
                {
                    var handler = serviceProvider.GetRequiredService<IEventHandler<InvoicingEvent>>();
                    await handler.HandleAsync(@event);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _channel!.CloseAsync();
            await _channel!.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
