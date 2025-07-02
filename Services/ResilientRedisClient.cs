using Azure.Identity;
using Azure.Redis.Resilient.Client.Configuration;
using Azure.Redis.Resilient.Client.Interfaces;
using Azure.Redis.Resilient.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace Azure.Redis.Resilient.Client.Services;

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

    public void Dispose()
    {
        if (_disposed)
            return;

        _redis?.Dispose();
        _disposed = true;
    }
}
