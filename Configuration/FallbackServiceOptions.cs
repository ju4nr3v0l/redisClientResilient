namespace ResilientRedis.Client.Configuration;

/// <summary>
/// Configuration options for fallback service behavior
/// </summary>
public class FallbackServiceOptions
{
    /// <summary>
    /// Configuration section name for fallback service options
    /// </summary>
    public const string SectionName = "FallbackService";
    
    /// <summary>
    /// URL base del servicio de fallback
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Timeout para llamadas HTTP en segundos
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Número máximo de reintentos para el servicio de fallback
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;
    
    /// <summary>
    /// Headers adicionales para las peticiones HTTP
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}
