using ResilientRedis.Client.Models;

namespace ResilientRedis.Client.Interfaces;

/// <summary>
/// Provides a resilient Redis client with automatic retry, fallback, and event publishing capabilities.
/// </summary>
public interface IResilientRedisClient
{
    /// <summary>
    /// Obtiene un valor del cache con resiliencia
    /// </summary>
    /// <typeparam name="T">Tipo del objeto a obtener</typeparam>
    /// <param name="key">Clave del cache</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado del cache con metadata</returns>
    Task<CacheResult<T?>> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene un valor del cache como string
    /// </summary>
    /// <param name="key">Clave del cache</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado del cache con el valor como string</returns>
    Task<CacheResult<string?>> GetStringAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Establece un valor en el cache
    /// </summary>
    /// <typeparam name="T">Tipo del objeto a guardar</typeparam>
    /// <param name="key">Clave del cache</param>
    /// <param name="value">Valor a guardar</param>
    /// <param name="expiration">Tiempo de expiración opcional</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación</returns>
    Task<CacheResult<bool>> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Establece un valor string en el cache
    /// </summary>
    /// <param name="key">Clave del cache</param>
    /// <param name="value">Valor string a guardar</param>
    /// <param name="expiration">Tiempo de expiración opcional</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación</returns>
    Task<CacheResult<bool>> SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Elimina una clave del cache
    /// </summary>
    /// <param name="key">Clave a eliminar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación</returns>
    Task<CacheResult<bool>> DeleteAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si existe una clave en el cache
    /// </summary>
    /// <param name="key">Clave a verificar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado indicando si la clave existe</returns>
    Task<CacheResult<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene o crea un valor usando el servicio de fallback
    /// </summary>
    /// <typeparam name="T">Tipo del objeto</typeparam>
    /// <param name="key">Clave del cache</param>
    /// <param name="fallbackFactory">Función para crear el valor si no existe en cache</param>
    /// <param name="expiration">Tiempo de expiración opcional</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado con el valor obtenido del cache o del fallback</returns>
    Task<CacheResult<T?>> GetOrCreateAsync<T>(string key, Func<Task<T>> fallbackFactory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalida múltiples claves por patrón
    /// </summary>
    /// <param name="pattern">Patrón de claves a invalidar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Número de claves eliminadas</returns>
    Task<CacheResult<long>> InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
}
