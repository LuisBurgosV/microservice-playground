using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Listener
{
    public interface IRabbitMQListener
    {
        Task StartListeningAsync(CancellationToken ct);
    }

    public class RabbitMQListener : IRabbitMQListener, IAsyncDisposable
    {
        private const string ExchangeName = "mp.events";
        private const string QueueName = "rabbitmq-ollama-queue";
        private const string RoutingKey = "rabbitmq.ollama";

        private readonly ConnectionFactory _factory;
        private readonly IOllamaService _ollamaService;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQListener(string host, string user, string pass, IOllamaService ollamaService)
        {
            _factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };
            _ollamaService = ollamaService;
        }

        public async Task StartListeningAsync(CancellationToken ct)
        {
            try
            {
                _connection = await _factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(null, ct);

                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    cancellationToken: ct);

                var queueDeclareOk = await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: ct);

                await _channel.QueueBindAsync(
                    queue: queueDeclareOk.QueueName,
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    cancellationToken: ct);

                Console.WriteLine("[RabbitMQ] Listener started, waiting for messages...");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var messageBody = Encoding.UTF8.GetString(ea.Body.ToArray());
                    Console.WriteLine($"[RabbitMQ] Received: {messageBody}");

                    try
                    {
                        var response = await _ollamaService.GenerateResponseAsync(messageBody, ct);

                        if (!string.IsNullOrEmpty(ea.BasicProperties?.ReplyTo))
                        {
                            var replyProps = new BasicProperties
                            {
                                CorrelationId = ea.BasicProperties.CorrelationId
                            };

                            await _channel.BasicPublishAsync(
                                exchange: string.Empty,
                                routingKey: ea.BasicProperties.ReplyTo,
                                mandatory: false,
                                basicProperties: replyProps,
                                body: Encoding.UTF8.GetBytes(response),
                                cancellationToken: ct);

                            Console.WriteLine($"[RabbitMQ] Response sent to: {ea.BasicProperties.ReplyTo}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RabbitMQ] Error processing message: {ex.Message}");
                    }

                    await Task.CompletedTask;
                };

                await _channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: true,
                    consumerTag: "rabbitmq-ollama-consumer",
                    noLocal: false,
                    exclusive: false,
                    arguments: null,
                    consumer: consumer,
                    cancellationToken: ct);

                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[RabbitMQ] Listener stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] Error: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
                await _channel.CloseAsync();

            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}
