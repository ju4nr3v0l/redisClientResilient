using System.Text.Json.Serialization;

namespace Azure.Redis.Resilient.Client.Models;

public class RedisEvent
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("eventType")]
    public RedisEventType EventType { get; set; }
    
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("source")]
    public string Source { get; set; } = "RedisClient";
    
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; }
    
    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum RedisEventType
{
    Get,
    Set,
    Delete,
    Exists,
    FallbackTriggered,
    CacheHit,
    CacheMiss,
    Error
}
