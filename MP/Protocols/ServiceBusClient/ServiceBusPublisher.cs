using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using System.Text;

namespace ServiceBusClient
{
    public interface IEventPublisher
    {
        Task<string> PublishAsync<T>(string queueName, string message, CancellationToken ct);
    }

    public class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private const string QueueName = "servicebus-ollama-queue";
        private const string ReplyQueueName = "servicebus-ollama-queue.replies";

        private readonly Azure.Messaging.ServiceBus.ServiceBusClient _client;
        private ServiceBusReceiver? _receiver;
        private ServiceBusSender? _sender;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingReplies = new();
        private string? _replyQueueName;

        public ServiceBusEventPublisher(string connectionString)
        {
            _client = new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
        }

        private async Task EnsureInitializedAsync(string queueName, CancellationToken ct)
        {
            if (_sender != null && _receiver != null && _replyQueueName != null)
                return;

            await _initLock.WaitAsync(ct);
            try
            {
                if (_sender is null)
                {
                    _sender = _client.CreateSender(QueueName);
                }

                if (_replyQueueName is null)
                {
                    _replyQueueName = ReplyQueueName;
                    _receiver = _client.CreateReceiver(_replyQueueName);

                    _ = Task.Run(() => ListenForRepliesAsync(ct), ct);
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task ListenForRepliesAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var message = await _receiver!.ReceiveMessageAsync(TimeSpan.FromSeconds(1), ct);

                    if (message != null)
                    {
                        var correlationId = message.CorrelationId ?? string.Empty;
                        var body = Encoding.UTF8.GetString(message.Body);

                        if (_pendingReplies.TryRemove(correlationId, out var tcs))
                        {
                            tcs.SetResult(body);
                        }

                        await _receiver.CompleteMessageAsync(message, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        public async Task<string> PublishAsync<T>(string queueName, string message, CancellationToken ct)
        {
            await EnsureInitializedAsync(queueName, ct);

            var correlationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            _pendingReplies[correlationId] = tcs;

            try
            {
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(message))
                {
                    CorrelationId = correlationId,
                    ReplyTo = _replyQueueName
                };

                await _sender!.SendMessageAsync(serviceBusMessage, ct);

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
            if (_receiver is not null)
                await _receiver.DisposeAsync();

            if (_sender is not null)
                await _sender.DisposeAsync();

            await _client.DisposeAsync();
        }
    }
}
