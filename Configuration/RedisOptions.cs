namespace ResilientRedis.Client.Configuration;

/// <summary>
/// Configuration options for Redis connection and behavior
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Configuration section name for Redis options
    /// </summary>
    public const string SectionName = "Redis";
    
    /// <summary>
    /// Connection string para Redis (usado cuando no se usa Managed Identity)
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Nombre del host de Redis (requerido cuando se usa Managed Identity)
    /// </summary>
    public string HostName { get; set; } = string.Empty;
    
    /// <summary>
    /// Puerto de Redis
    /// </summary>
    public int Port { get; set; } = 6380;
    
    /// <summary>
    /// Usar SSL
    /// </summary>
    public bool UseSsl { get; set; } = true;
    
    /// <summary>
    /// Usar Managed Identity para autenticación (opcional)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;
    
    /// <summary>
    /// Tiempo de expiración por defecto en minutos
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Prefijo para las claves de Redis
    /// </summary>
    public string KeyPrefix { get; set; } = "app";
    
    /// <summary>
    /// Número máximo de reintentos
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Tiempo de espera base para reintentos en segundos
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;
}
