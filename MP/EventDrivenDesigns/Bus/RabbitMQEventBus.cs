using System.Text;
using System.Text.Json;
using EventDrivenDesigns.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventDrivenDesigns.Bus
{
    public class RabbitMQEventBus : IEventBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName = "event_driven_exchange";

        public RabbitMQEventBus(string hostName = "localhost", int port = 5672, string userName = "admin", string password = "admin")
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = hostName,
                    Port = port,
                    UserName = userName,
                    Password = password
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);

                Console.WriteLine("[OK] Conectado a RabbitMQ en {0}:{1}", hostName, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Conectando a RabbitMQ: {0}", ex.Message);
                throw;
            }
        }

        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent
        {
            var eventType = typeof(TEvent).Name;
            var handlerType = handler.GetType().Name;
            var queueName = $"{eventType}_{handlerType}";  // Reutilizable
            var routingKey = eventType;
            var consumerTag = $"{eventType}_{handlerType}_{Guid.NewGuid():N}";  // Único temporal

            try
            {
                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queue: queueName, exchange: _exchangeName, routingKey: routingKey);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var @event = JsonSerializer.Deserialize<TEvent>(json);

                        if (@event != null)
                        {
                            handler.HandleAsync(@event).Wait();
                        }

                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[ERROR] Procesando evento: {0}", ex.Message);
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumerTag: consumerTag, consumer: consumer);
                Console.WriteLine("[OK] Handler suscrito: {0}", handlerType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Suscribiendo: {0}", ex.Message);
                throw;
            }
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
        {
            var routingKey = typeof(TEvent).Name;

            try
            {
                var json = JsonSerializer.Serialize(@event);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(_exchangeName, routingKey, basicProperties: properties, body: body);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Publicando evento: {0}", ex.Message);
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }

        public void DeleteAllQueues()
        {
            try
            {
                var queueNames = new[]
                {
                    "BetPlacedEvent_BetPlacedOddsUpdateHandler",
                    "BetPlacedEvent_BetPlacedNotificationHandler",
                    "BetPlacedEvent_BetPlacedAuditHandler",
                    "BetWonEvent_BetWonPaymentHandler",
                    "BetWonEvent_BetWonNotificationHandler",
                    "BetLostEvent_BetLostStatisticsHandler",
                    "OddsUpdatedEvent_OddsUpdatedNotificationHandler",
                    "EventResultAnnouncedEvent_EventResultProcessingHandler"
                };

                foreach (var queueName in queueNames)
                {
                    try
                    {
                        _channel.QueueDelete(queueName);
                        Console.WriteLine("[OK] Cola eliminada: {0}", queueName);
                    }
                    catch
                    {
                        // Cola no existe, ignorar
                    }
                }

                Console.WriteLine("[OK] Todas las colas han sido eliminadas\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Eliminando colas: {0}", ex.Message);
            }
        }
    }
}
