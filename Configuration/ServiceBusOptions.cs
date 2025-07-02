namespace Azure.Redis.Resilient.Client.Configuration;

public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";
    
    /// <summary>
    /// Connection string de Service Bus
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Namespace de Service Bus
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del tópico para eventos de Redis
    /// </summary>
    public string RedisEventsTopic { get; set; } = "redis-events";
    
    /// <summary>
    /// Usar Managed Identity para Service Bus
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
    
    /// <summary>
    /// Habilitar publicación de eventos
    /// </summary>
    public bool EnableEventPublishing { get; set; } = true;
}
