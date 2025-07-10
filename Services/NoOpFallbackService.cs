using ResilientRedis.Client.Interfaces;

namespace RedisNuget.Services;
public class NoOpFallbackService : IFallbackService
{
    public Task<T?> CreateDataAsync<T>(string key, object parameters, CancellationToken cancellationToken = default)
    {
        // Implementación dummy: no hace nada, retorna valor por defecto (null)
        return Task.FromResult<T?>(default);
    }

    public Task<T?> GetDataAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Implementación dummy: no hace nada, retorna valor por defecto (null)
        return Task.FromResult<T?>(default);
    }
}

