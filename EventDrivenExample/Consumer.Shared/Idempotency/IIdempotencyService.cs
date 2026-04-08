using StackExchange.Redis;

namespace Consumer.Shared.Idempotency;

/// <summary>
/// Servicio de idempotencia usando Redis
/// Previene el procesamiento duplicado de eventos
/// </summary>
public interface IIdempotencyService
{
    Task<bool> IsProcessedAsync(string eventId);
    Task MarkAsProcessedAsync(string eventId, TimeSpan? expiration = null);
}

public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromDays(7);
    private const string KeyPrefix = "event-processed:";

    public RedisIdempotencyService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<bool> IsProcessedAsync(string eventId)
    {
        var key = $"{KeyPrefix}{eventId}";
        var exists = await _database.StringGetAsync(key);
        return exists.HasValue;
    }

    public async Task MarkAsProcessedAsync(string eventId, TimeSpan? expiration = null)
    {
        var key = $"{KeyPrefix}{eventId}";
        var ttl = expiration ?? _defaultExpiration;
        
        await _database.StringSetAsync(key, "processed", ttl);
    }
}
