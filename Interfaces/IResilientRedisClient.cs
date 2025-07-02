using Azure.Redis.Resilient.Client.Models;

namespace Azure.Redis.Resilient.Client.Interfaces;

public interface IResilientRedisClient
{
    /// <summary>
    /// Obtiene un valor del cache con resiliencia
    /// </summary>
    Task<CacheResult<T?>> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene un valor del cache como string
    /// </summary>
    Task<CacheResult<string?>> GetStringAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Establece un valor en el cache
    /// </summary>
    Task<CacheResult<bool>> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Establece un valor string en el cache
    /// </summary>
    Task<CacheResult<bool>> SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Elimina una clave del cache
    /// </summary>
    Task<CacheResult<bool>> DeleteAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si existe una clave en el cache
    /// </summary>
    Task<CacheResult<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene o crea un valor usando el servicio de fallback
    /// </summary>
    Task<CacheResult<T?>> GetOrCreateAsync<T>(string key, Func<Task<T>> fallbackFactory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalida múltiples claves por patrón
    /// </summary>
    Task<CacheResult<long>> InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
}
