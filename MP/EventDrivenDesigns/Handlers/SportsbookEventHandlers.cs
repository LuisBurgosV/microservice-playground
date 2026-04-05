using EventDrivenDesigns.Bus;
using EventDrivenDesigns.Events;

namespace EventDrivenDesigns.Handlers
{
    public class BetPlacedOddsUpdateHandler : IEventHandler<BetPlacedEvent>
    {
        private readonly IEventBus _eventBus;

        public BetPlacedOddsUpdateHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task HandleAsync(BetPlacedEvent @event)
        {
            await Task.Run(async () =>
            {
                Console.WriteLine($"[ODDS] Cuotas actualizadas para: {@event.SportEvent}");

                var oddsUpdatedEvent = new OddsUpdatedEvent(
                    sportEvent: @event.SportEvent,
                    outcomes: new Dictionary<string, decimal>
                    {
                        { "Home Win", 1.5m },
                        { "Away Win", 2.8m },
                        { "Draw", 3.2m }
                    }
                );

                await _eventBus.PublishAsync(oddsUpdatedEvent);
            });
        }
    }

    public class BetPlacedNotificationHandler : IEventHandler<BetPlacedEvent>
    {
        public async Task HandleAsync(BetPlacedEvent @event)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"[NOTIFY] Apuesta confirmada: ${@event.Amount} en {@event.PredictedOutcome}");
            });
        }
    }

    public class BetPlacedAuditHandler : IEventHandler<BetPlacedEvent>
    {
        public async Task HandleAsync(BetPlacedEvent @event)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"[AUDIT] Transacción registrada: Usuario {@event.UserId}, ${@event.Amount}");
            });
        }
    }

    public class BetWonPaymentHandler : IEventHandler<BetWonEvent>
    {
        public async Task HandleAsync(BetWonEvent @event)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"[PAYMENT] Pago procesado: ${@event.WinningsAmount}");
            });
        }
    }

    public class BetWonNotificationHandler : IEventHandler<BetWonEvent>
    {
        public async Task HandleAsync(BetWonEvent @event)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"[NOTIFY] !GANASTE! ${@event.WinningsAmount}");
            });
        }
    }

    public class BetLostStatisticsHandler : IEventHandler<BetLostEvent>
    {
        public async Task HandleAsync(BetLostEvent @event)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"[STATS] Apuesta perdida: ${@event.Amount}");
            });
        }
    }

    public class OddsUpdatedNotificationHandler : IEventHandler<OddsUpdatedEvent>
    {
        public async Task HandleAsync(OddsUpdatedEvent @event)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"[NOTIFY] Cuotas actualizadas: {@event.SportEvent}");
            });
        }
    }

    public class EventResultProcessingHandler : IEventHandler<EventResultAnnouncedEvent>
    {
        private readonly IEventBus _eventBus;

        public EventResultProcessingHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task HandleAsync(EventResultAnnouncedEvent @event)
        {
            await Task.Run(async () =>
            {
                Console.WriteLine($"[RESULT] Resultado procesado: {@event.SportEvent} -> {@event.WinningOutcome}");

                var betWonEvent = new BetWonEvent(
                    betId: 1,
                    userId: 123,
                    amount: 100m,
                    odds: 1.8m,
                    winningsAmount: 180m
                );

                await _eventBus.PublishAsync(betWonEvent);
            });
        }
    }
}
