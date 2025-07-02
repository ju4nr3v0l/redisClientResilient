using ResilientRedis.Client.Models;

namespace ResilientRedis.Client.Interfaces;

/// <summary>
/// Interface for publishing Redis events to external systems
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publica un evento de Redis en Service Bus
    /// </summary>
    Task PublishAsync(RedisEvent redisEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publica múltiples eventos en batch
    /// </summary>
    Task PublishBatchAsync(IEnumerable<RedisEvent> events, CancellationToken cancellationToken = default);
}
