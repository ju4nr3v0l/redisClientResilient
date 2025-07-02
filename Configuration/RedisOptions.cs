namespace Azure.Redis.Resilient.Client.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";
    
    /// <summary>
    /// Connection string para Redis (puede incluir Managed Identity)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del host de Redis
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
    /// Usar Managed Identity para autenticación
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
    
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
