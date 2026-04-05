using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;

namespace RabbitMQClient
{
    public interface IEventPublisher
    {
        Task<string> PublishAsync<T>(string routingKey, string message, CancellationToken ct);
    }

    public class RabbitMQPublisher : IEventPublisher, IAsyncDisposable
    {
        private const string ExchangeName = "mp.events";
        private const string QueueName = "rabbitmq-ollama-queue";
        private const string RoutingKey = "rabbitmq.ollama";

        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingReplies = new();
        private string? _replyQueueName;

        public RabbitMQPublisher(string host, string user, string pass)
        {
            _factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };
        }

        private async Task EnsureInitializedAsync(CancellationToken ct)
        {
            if (_connection != null && _channel != null && _replyQueueName != null)
                return;

            await _initLock.WaitAsync(ct);
            try
            {
                if (_connection is null)
                {
                    _connection = await _factory.CreateConnectionAsync(ct);
                }

                if (_channel is null)
                {
                    _channel = await _connection.CreateChannelAsync(null, ct);
                }

                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true, 
                    cancellationToken: ct);

                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: ct);

                await _channel.QueueBindAsync(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    cancellationToken: ct);

                if (_replyQueueName is null)
                {
                    var queueDeclareOk = await _channel.QueueDeclareAsync(
                        queue: string.Empty,
                        durable: false,
                        exclusive: true,
                        autoDelete: true,
                        arguments: null,
                        cancellationToken: ct);

                    _replyQueueName = queueDeclareOk.QueueName;

                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var correlationId = ea.BasicProperties?.CorrelationId ?? string.Empty;
                        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                        if (_pendingReplies.TryRemove(correlationId, out var tcs))
                        {
                            tcs.SetResult(body);
                        }

                        await Task.CompletedTask;
                    };

                    await _channel.BasicConsumeAsync(
                        queue: _replyQueueName,
                        autoAck: true,
                        consumerTag: "reply-consumer",
                        noLocal: false,
                        exclusive: false,
                        arguments: null,
                        consumer: consumer,
                        cancellationToken: ct);
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<string> PublishAsync<T>(string routingKey, string message, CancellationToken ct)
        {
            await EnsureInitializedAsync(ct);

            var correlationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            _pendingReplies[correlationId] = tcs;

            try
            {
                var body = Encoding.UTF8.GetBytes(message);

                var props = new BasicProperties
                {
                    Persistent = true,
                    CorrelationId = correlationId,
                    ReplyTo = _replyQueueName
                };

                await _channel!.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body,
                    cancellationToken: ct);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _pendingReplies.TryRemove(correlationId, out _);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
                await _channel.CloseAsync();

            if (_connection is not null)
                await _connection.CloseAsync();

            _initLock.Dispose();
        }
    }
}
