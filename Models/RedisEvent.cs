using System.Text.Json.Serialization;

namespace ResilientRedis.Client.Models;

/// <summary>
/// Represents an event that occurred during Redis operations
/// </summary>
public class RedisEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of Redis event that occurred
    /// </summary>
    [JsonPropertyName("eventType")]
    public RedisEventType EventType { get; set; }
    
    /// <summary>
    /// Redis key involved in the operation
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Value associated with the operation (if applicable)
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Source of the event (typically the service name)
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "RedisClient";
    
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Indicates whether fallback service was used
    /// </summary>
    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; }
    
    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Additional metadata for the event
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of Redis events that can occur
/// </summary>
public enum RedisEventType
{
    /// <summary>
    /// Get operation performed
    /// </summary>
    Get,
    /// <summary>
    /// Set operation performed
    /// </summary>
    Set,
    /// <summary>
    /// Delete operation performed
    /// </summary>
    Delete,
    /// <summary>
    /// Exists check performed
    /// </summary>
    Exists,
    /// <summary>
    /// Fallback service was triggered
    /// </summary>
    FallbackTriggered,
    /// <summary>
    /// Cache hit occurred
    /// </summary>
    CacheHit,
    /// <summary>
    /// Cache miss occurred
    /// </summary>
    CacheMiss,
    /// <summary>
    /// Error occurred during operation
    /// </summary>
    Error
}
