using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Listener
{
    public class ListenerService : BackgroundService
    {
        private readonly IRabbitMQListener _rabbitMQListener;
        private readonly IServiceBusListener _serviceBusListener;
        private readonly ILogger<ListenerService> _logger;

        public ListenerService(
            IRabbitMQListener rabbitMQListener,
            IServiceBusListener serviceBusListener,
            ILogger<ListenerService> logger)
        {
            _rabbitMQListener = rabbitMQListener;
            _serviceBusListener = serviceBusListener;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event listeners service is starting...");

            try
            {
                // Start both listeners in parallel
                var rabbitTask = _rabbitMQListener.StartListeningAsync(stoppingToken);
                var serviceBusTask = _serviceBusListener.StartListeningAsync(stoppingToken);

                await Task.WhenAll(rabbitTask, serviceBusTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the event listeners service");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event listeners service is stopping...");

            if (_rabbitMQListener is IAsyncDisposable rabbitDisposable)
                await rabbitDisposable.DisposeAsync();

            if (_serviceBusListener is IAsyncDisposable serviceBusDisposable)
                await serviceBusDisposable.DisposeAsync();

            await base.StopAsync(cancellationToken);
        }
    }
}
