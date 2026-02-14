using Azure.Messaging.ServiceBus;
using System.Text;

namespace Listener
{
    public interface IServiceBusListener
    {
        Task StartListeningAsync(CancellationToken ct);
    }

    public class ServiceBusListener : IServiceBusListener, IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly string _queueName = "servicebus-ollama-queue";
        private readonly string _replyQueueName = "servicebus-ollama-queue.replies";
        private readonly IOllamaService _ollamaService;
        private Azure.Messaging.ServiceBus.ServiceBusClient? _client;
        private ServiceBusReceiver? _receiver;
        private ServiceBusSender? _replySender;

        public ServiceBusListener(string connectionString, IOllamaService ollamaService)
        {
            _connectionString = connectionString;
            _ollamaService = ollamaService;
        }

        public async Task StartListeningAsync(CancellationToken ct)
        {
            try
            {
                _client = new Azure.Messaging.ServiceBus.ServiceBusClient(_connectionString);
                _receiver = _client.CreateReceiver(_queueName);
                _replySender = _client.CreateSender(_replyQueueName);

                Console.WriteLine("[ServiceBus] Listener started, waiting for messages...");

                while (!ct.IsCancellationRequested)
                {
                    var message = await _receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1), ct);

                    if (message != null)
                    {
                        var messageBody = Encoding.UTF8.GetString(message.Body);
                        Console.WriteLine($"[ServiceBus] Received: {messageBody}");

                        try
                        {
                            var response = await _ollamaService.GenerateResponseAsync(messageBody, ct);

                            if (!string.IsNullOrEmpty(message.ReplyTo))
                            {
                                var replyMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(response))
                                {
                                    CorrelationId = message.CorrelationId
                                };

                                await _replySender.SendMessageAsync(replyMessage, ct);
                                Console.WriteLine($"[ServiceBus] Response sent to: {message.ReplyTo}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ServiceBus] Error processing message: {ex.Message}");
                        }

                        await _receiver.CompleteMessageAsync(message, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[ServiceBus] Listener stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServiceBus] Error: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_receiver is not null)
                await _receiver.DisposeAsync();

            if (_replySender is not null)
                await _replySender.DisposeAsync();

            if (_client is not null)
                await _client.DisposeAsync();
        }
    }
}
