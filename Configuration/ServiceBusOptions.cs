namespace ResilientRedis.Client.Configuration;

/// <summary>
/// Configuration options for Azure Service Bus integration
/// </summary>
public class ServiceBusOptions
{
    /// <summary>
    /// Configuration section name for Service Bus options
    /// </summary>
    public const string SectionName = "ServiceBus";
    
    /// <summary>
    /// Connection string de Service Bus (usado cuando no se usa Managed Identity)
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Namespace de Service Bus (requerido cuando se usa Managed Identity)
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del tópico para eventos de Redis
    /// </summary>
    public string RedisEventsTopic { get; set; } = "redis-events";
    
    /// <summary>
    /// Usar Managed Identity para Service Bus (opcional)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;
    
    /// <summary>
    /// Habilitar publicación de eventos (opcional)
    /// </summary>
    public bool EnableEventPublishing { get; set; } = false;
}
