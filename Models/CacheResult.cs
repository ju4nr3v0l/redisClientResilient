namespace Azure.Redis.Resilient.Client.Models;

public class CacheResult<T>
{
    public T? Value { get; set; }
    public bool Success { get; set; }
    public bool FromCache { get; set; }
    public bool FromFallback { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    
    public static CacheResult<T> SuccessFromCache(T value, TimeSpan executionTime)
    {
        return new CacheResult<T>
        {
            Value = value,
            Success = true,
            FromCache = true,
            FromFallback = false,
            ExecutionTime = executionTime
        };
    }
    
    public static CacheResult<T> SuccessFromFallback(T value, TimeSpan executionTime)
    {
        return new CacheResult<T>
        {
            Value = value,
            Success = true,
            FromCache = false,
            FromFallback = true,
            ExecutionTime = executionTime
        };
    }
    
    public static CacheResult<T> Failure(string errorMessage, TimeSpan executionTime)
    {
        return new CacheResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ExecutionTime = executionTime
        };
    }
}
