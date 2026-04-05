namespace EventDrivenDesigns.Events
{
    /// <summary>
    /// Evento: Una apuesta fue colocada
    /// </summary>
    public class BetPlacedEvent : DomainEvent
    {
        public int BetId { get; set; }
        public int UserId { get; set; }
        public string SportEvent { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Odds { get; set; }
        public string PredictedOutcome { get; set; } = string.Empty; // "Home Win", "Away Win", "Draw"
        public string Status { get; set; } = "Pending";

        public BetPlacedEvent(int betId, int userId, string sportEvent, decimal amount, decimal odds, string predictedOutcome)
        {
            BetId = betId;
            UserId = userId;
            SportEvent = sportEvent;
            Amount = amount;
            Odds = odds;
            PredictedOutcome = predictedOutcome;
        }
    }

    /// <summary>
    /// Evento: Una apuesta ganó
    /// </summary>
    public class BetWonEvent : DomainEvent
    {
        public int BetId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public decimal Odds { get; set; }
        public decimal WinningsAmount { get; set; } // Amount * Odds
        public DateTime WonAt { get; set; }

        public BetWonEvent(int betId, int userId, decimal amount, decimal odds, decimal winningsAmount)
        {
            BetId = betId;
            UserId = userId;
            Amount = amount;
            Odds = odds;
            WinningsAmount = winningsAmount;
            WonAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Evento: Una apuesta perdió
    /// </summary>
    public class BetLostEvent : DomainEvent
    {
        public int BetId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime LostAt { get; set; }

        public BetLostEvent(int betId, int userId, decimal amount)
        {
            BetId = betId;
            UserId = userId;
            Amount = amount;
            LostAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Evento: Las cuotas/odds fueron actualizadas
    /// </summary>
    public class OddsUpdatedEvent : DomainEvent
    {
        public string SportEvent { get; set; } = string.Empty;
        public Dictionary<string, decimal> Outcomes { get; set; } = new(); // "Home Win": 1.5, "Away Win": 2.0, "Draw": 3.0
        public DateTime UpdatedAt { get; set; }

        public OddsUpdatedEvent(string sportEvent, Dictionary<string, decimal> outcomes)
        {
            SportEvent = sportEvent;
            Outcomes = outcomes;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Evento: El resultado de un evento deportivo fue anunciado
    /// </summary>
    public class EventResultAnnouncedEvent : DomainEvent
    {
        public string SportEvent { get; set; } = string.Empty;
        public string WinningOutcome { get; set; } = string.Empty; // "Home Win", "Away Win", "Draw"
        public DateTime ResultAt { get; set; }

        public EventResultAnnouncedEvent(string sportEvent, string winningOutcome)
        {
            SportEvent = sportEvent;
            WinningOutcome = winningOutcome;
            ResultAt = DateTime.UtcNow;
        }
    }
}
