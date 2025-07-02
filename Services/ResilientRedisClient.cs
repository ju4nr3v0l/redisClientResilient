using Azure.Identity;
using ResilientRedis.Client.Configuration;
using ResilientRedis.Client.Interfaces;
using ResilientRedis.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace ResilientRedis.Client.Services;

/// <summary>
/// Resilient Redis client with fallback capabilities and event publishing
/// </summary>
public class ResilientRedisClient : IResilientRedisClient, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly IFallbackService _fallbackService;
    private readonly IEventPublisher _eventPublisher;
    private readonly RedisOptions _options;
    private readonly ILogger<ResilientRedisClient> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the ResilientRedisClient class
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    /// <param name="fallbackService">Fallback service for when Redis is unavailable</param>
    /// <param name="eventPublisher">Event publisher for Redis operations</param>
    /// <param name="options">Redis configuration options</param>
    /// <param name="logger">Logger instance</param>
    public ResilientRedisClient(
        IConnectionMultiplexer redis,
        IFallbackService fallbackService,
        IEventPublisher eventPublisher,
        IOptions<RedisOptions> options,
        ILogger<ResilientRedisClient> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _fallbackService = fallbackService;
        _eventPublisher = eventPublisher;
        _options = options.Value;
        _logger = logger;

        // Configurar pipeline de resiliencia
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<RedisException>().Handle<TimeoutException>(),
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    /// <summary>
    /// Gets a value from Redis cache with fallback support
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result containing the value and metadata</returns>
    public async Task<CacheResult<T?>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullKey = GetFullKey(key);

        try
        {
            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                var value = await _database.StringGetAsync(fullKey);
                return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
            }, cancellationToken);

            stopwatch.Stop();

            var cacheResult = CacheResult<T?>.SuccessFromCache(result, stopwatch.Elapsed);
            
            await PublishEventAsync(new RedisEvent
            {
                EventType = result != null ? RedisEventType.CacheHit : RedisEventType.CacheMiss,
                Key = key,
                Success = true,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return cacheResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting value from Redis for key: {Key}", key);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = key,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<T?>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Gets a string value from Redis cache with fallback support
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result containing the string value and metadata</returns>
    public async Task<CacheResult<string?>> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullKey = GetFullKey(key);

        try
        {
            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                var value = await _database.StringGetAsync(fullKey);
                return value.HasValue ? value.ToString() : null;
            }, cancellationToken);

            stopwatch.Stop();

            var cacheResult = CacheResult<string?>.SuccessFromCache(result, stopwatch.Elapsed);
            
            await PublishEventAsync(new RedisEvent
            {
                EventType = result != null ? RedisEventType.CacheHit : RedisEventType.CacheMiss,
                Key = key,
                Success = true,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return cacheResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting string value from Redis for key: {Key}", key);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = key,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<string?>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Sets a value in Redis cache
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result indicating success or failure</returns>
    public async Task<CacheResult<bool>> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullKey = GetFullKey(key);
        var exp = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        try
        {
            var json = JsonSerializer.Serialize(value);
            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                return await _database.StringSetAsync(fullKey, json, exp);
            }, cancellationToken);

            stopwatch.Stop();

            var cacheResult = CacheResult<bool>.SuccessFromCache(result, stopwatch.Elapsed);
            
            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Set,
                Key = key,
                Value = json,
                Success = result,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return cacheResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting value in Redis for key: {Key}", key);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = key,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<bool>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Sets a string value in Redis cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">String value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result indicating success or failure</returns>
    public async Task<CacheResult<bool>> SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullKey = GetFullKey(key);
        var exp = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        try
        {
            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                return await _database.StringSetAsync(fullKey, value, exp);
            }, cancellationToken);

            stopwatch.Stop();

            var cacheResult = CacheResult<bool>.SuccessFromCache(result, stopwatch.Elapsed);
            
            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Set,
                Key = key,
                Value = value,
                Success = result,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return cacheResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting string value in Redis for key: {Key}", key);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = key,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<bool>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Deletes a value from Redis cache
    /// </summary>
    /// <param name="key">Cache key to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result indicating success or failure</returns>
    public async Task<CacheResult<bool>> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullKey = GetFullKey(key);

        try
        {
            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                return await _database.KeyDeleteAsync(fullKey);
            }, cancellationToken);

            stopwatch.Stop();

            var cacheResult = CacheResult<bool>.SuccessFromCache(result, stopwatch.Elapsed);
            
            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Delete,
                Key = key,
                Success = result,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return cacheResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error deleting key from Redis: {Key}", key);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = key,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<bool>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Checks if a key exists in Redis cache
    /// </summary>
    /// <param name="key">Cache key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result indicating if the key exists</returns>
    public async Task<CacheResult<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullKey = GetFullKey(key);

        try
        {
            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                return await _database.KeyExistsAsync(fullKey);
            }, cancellationToken);

            stopwatch.Stop();

            var cacheResult = CacheResult<bool>.SuccessFromCache(result, stopwatch.Elapsed);
            
            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Exists,
                Key = key,
                Success = true,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return cacheResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error checking if key exists in Redis: {Key}", key);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = key,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<bool>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Gets a value from cache or creates it using the provided factory function
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="fallbackFactory">Function to create the value if not found in cache</param>
    /// <param name="expiration">Optional expiration time for the cached value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result containing the value and metadata</returns>
    public async Task<CacheResult<T?>> GetOrCreateAsync<T>(string key, Func<Task<T>> fallbackFactory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Primero intentar obtener de Redis
        var cacheResult = await GetAsync<T>(key, cancellationToken);
        
        if (cacheResult.Success && cacheResult.Value != null)
        {
            return cacheResult;
        }

        // Si no está en cache, usar el fallback
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Cache miss for key {Key}, using fallback", key);
            
            var fallbackValue = await fallbackFactory();
            
            if (fallbackValue != null)
            {
                // Guardar en Redis para futuras consultas
                var setResult = await SetAsync(key, fallbackValue, expiration, cancellationToken);
                
                stopwatch.Stop();

                await PublishEventAsync(new RedisEvent
                {
                    EventType = RedisEventType.FallbackTriggered,
                    Key = key,
                    Success = true,
                    FallbackUsed = true,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                });

                return CacheResult<T?>.SuccessFromFallback(fallbackValue, stopwatch.Elapsed);
            }

            stopwatch.Stop();
            return CacheResult<T?>.Failure("Fallback returned null", stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in fallback for key: {Key}", key);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = key,
                Success = false,
                ErrorMessage = ex.Message,
                FallbackUsed = true,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<T?>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Invalidates all cache entries matching the specified pattern
    /// </summary>
    /// <param name="pattern">Pattern to match cache keys (supports wildcards)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache result containing the number of invalidated keys</returns>
    public async Task<CacheResult<long>> InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullPattern = GetFullKey(pattern);

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: fullPattern).ToArray();
            
            if (keys.Length == 0)
            {
                stopwatch.Stop();
                return CacheResult<long>.SuccessFromCache(0, stopwatch.Elapsed);
            }

            var result = await _database.KeyDeleteAsync(keys);
            
            stopwatch.Stop();

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Delete,
                Key = pattern,
                Success = true,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Metadata = new Dictionary<string, object> { ["DeletedCount"] = result }
            });

            return CacheResult<long>.SuccessFromCache(result, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating pattern: {Pattern}", pattern);

            await PublishEventAsync(new RedisEvent
            {
                EventType = RedisEventType.Error,
                Key = pattern,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });

            return CacheResult<long>.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    private string GetFullKey(string key)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}:{key}";
    }

    private async Task PublishEventAsync(RedisEvent redisEvent)
    {
        try
        {
            await _eventPublisher.PublishAsync(redisEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish Redis event");
        }
    }

    /// <summary>
    /// Disposes the ResilientRedisClient and releases resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _redis?.Dispose();
        _disposed = true;
    }
}
