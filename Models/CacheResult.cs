namespace Azure.Redis.Resilient.Client.Models;

/// <summary>
/// Represents the result of a cache operation with metadata about the execution.
/// </summary>
/// <typeparam name="T">The type of the cached value</typeparam>
public class CacheResult<T>
{
    /// <summary>
    /// Gets or sets the cached value.
    /// </summary>
    public T? Value { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the value was retrieved from cache.
    /// </summary>
    public bool FromCache { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the value was retrieved from the fallback service.
    /// </summary>
    public bool FromFallback { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the execution time of the operation.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
    
    /// <summary>
    /// Creates a successful result from cache.
    /// </summary>
    /// <param name="value">The cached value</param>
    /// <param name="executionTime">The execution time</param>
    /// <returns>A successful cache result</returns>
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
    
    /// <summary>
    /// Creates a successful result from fallback service.
    /// </summary>
    /// <param name="value">The fallback value</param>
    /// <param name="executionTime">The execution time</param>
    /// <returns>A successful fallback result</returns>
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
    
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="executionTime">The execution time</param>
    /// <returns>A failed result</returns>
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
