using EventDrivenDesigns.Bus;
using EventDrivenDesigns.Events;
using EventDrivenDesigns.Handlers;

namespace EventDrivenDesigns
{
    /// <summary>
    /// EJEMPLO EDUCATIVO: Arquitectura Event-Driven con RabbitMQ
    /// </summary>
    public class SportsbookEventDrivenExample
    {
        private readonly IEventBus _eventBus;

        public SportsbookEventDrivenExample(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task RunExampleAsync()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          EVENT-DRIVEN ARCHITECTURE - RABBITMQ EXAMPLE          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            RegisterHandlers();

            await Task.Delay(1000);

            Console.WriteLine("\n1. ESCENARIO: Usuario coloca apuesta\n");
            await ScenarioUserPlacesBet();

            await Task.Delay(1000);

            Console.WriteLine("\n2. ESCENARIO: Se anuncia resultado\n");
            await ScenarioEventResultAnnounced();

            await Task.Delay(1000);

            Console.WriteLine("\n[OK] Ejemplo completado\n");
        }

        private void RegisterHandlers()
        {
            _eventBus.Subscribe<BetPlacedEvent>(new BetPlacedOddsUpdateHandler(_eventBus));
            _eventBus.Subscribe<BetPlacedEvent>(new BetPlacedNotificationHandler());
            _eventBus.Subscribe<BetPlacedEvent>(new BetPlacedAuditHandler());

            _eventBus.Subscribe<BetWonEvent>(new BetWonPaymentHandler());
            _eventBus.Subscribe<BetWonEvent>(new BetWonNotificationHandler());

            _eventBus.Subscribe<BetLostEvent>(new BetLostStatisticsHandler());

            _eventBus.Subscribe<OddsUpdatedEvent>(new OddsUpdatedNotificationHandler());

            _eventBus.Subscribe<EventResultAnnouncedEvent>(new EventResultProcessingHandler(_eventBus));

            Console.WriteLine("Handlers registrados en RabbitMQ [OK]");
        }

        private async Task ScenarioUserPlacesBet()
        {
            Console.WriteLine("  Usuario #123 coloca apuesta: $100 en 'Real Madrid vs Barcelona'");
            Console.WriteLine("  Prediccion: 'Home Win', Cuotas: 1.8x\n");

            var betPlacedEvent = new BetPlacedEvent(
                betId: 1,
                userId: 123,
                sportEvent: "Real Madrid vs Barcelona",
                amount: 100m,
                odds: 1.8m,
                predictedOutcome: "Home Win"
            );

            await _eventBus.PublishAsync(betPlacedEvent);
        }

        private async Task ScenarioEventResultAnnounced()
        {
            Console.WriteLine("  Resultado: Real Madrid gano 2-1 ('Home Win')\n");

            var eventResultEvent = new EventResultAnnouncedEvent(
                sportEvent: "Real Madrid vs Barcelona",
                winningOutcome: "Home Win"
            );

            await _eventBus.PublishAsync(eventResultEvent);
        }
    }
}
