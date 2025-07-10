using ResilientRedis.Client.Interfaces;
using ResilientRedis.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisNuget.Services;
public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(RedisEvent redisEvent, CancellationToken cancellationToken = default)
    {
        // Implementación dummy: no hace nada.
        return Task.CompletedTask;
    }

    public Task PublishBatchAsync(IEnumerable<RedisEvent> events, CancellationToken cancellationToken = default)
    {
        // Implementación dummy: no hace nada.
        return Task.CompletedTask;
    }
}

