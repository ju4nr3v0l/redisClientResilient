namespace Azure.Redis.Resilient.Client.Interfaces;

public interface IFallbackService
{
    /// <summary>
    /// Obtiene datos del servicio de fallback
    /// </summary>
    Task<T?> GetDataAsync<T>(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Crea datos en el servicio de fallback
    /// </summary>
    Task<T?> CreateDataAsync<T>(string key, object parameters, CancellationToken cancellationToken = default);
}
