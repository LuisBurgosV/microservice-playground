using Consumer.Shared.Handlers;
using Event.Shared.Events;
using Microsoft.Extensions.Logging;

namespace Consumer.Invoicing.Handlers
{
    public class InvoicingEventHandler : IEventHandler<InvoicingEvent>
    {
        private readonly ILogger<InvoicingEventHandler> _logger;

        public InvoicingEventHandler(ILogger<InvoicingEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(InvoicingEvent @event)
        {
            _logger.LogInformation(
            "📝 Registrando evento de facturacion creado - ID: {InvoiceId}, Order: {OrderId}, Amount: {Amount}",
            @event.InvoiceId,
            @event.OrderId,
            @event.Amount
        );

            // Simular almacenamiento en base de datos
            await Task.Delay(500);

            _logger.LogInformation(
                "✅ Evento de facturacion registrado en base de datos"
            );
        }
    }
}
