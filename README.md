# Resilient Redis Client

Un cliente resiliente para Azure Cache for Redis con soporte para Managed Identity, Service Bus events y patrones de fallback.

## Características

- ✅ **Resiliencia**: Reintentos automáticos con backoff exponencial
- ✅ **Managed Identity**: Autenticación segura sin connection strings
- ✅ **Service Bus Integration**: Publicación automática de eventos
- ✅ **Fallback Service**: Sistema de respaldo cuando Redis no está disponible
- ✅ **Logging**: Logging estructurado con diferentes niveles
- ✅ **Métricas**: Seguimiento de rendimiento y eventos
- ✅ **Configuración flexible**: Múltiples formas de configuración

## Instalación

```bash
dotnet add package ResilientRedis.Client
```

## Configuración

### appsettings.json

```json
{
  "Redis": {
    "HostName": "your-redis-cache.redis.cache.windows.net",
    "Port": 6380,
    "UseSsl": true,
    "UseManagedIdentity": true,
    "DefaultExpirationMinutes": 60,
    "KeyPrefix": "myapp",
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 2
  },
  "ServiceBus": {
    "Namespace": "your-servicebus-namespace",
    "RedisEventsTopic": "redis-events",
    "UseManagedIdentity": true,
    "EnableEventPublishing": true
  },
  "FallbackService": {
    "BaseUrl": "https://your-api.azurewebsites.net",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 2,
    "Headers": {
      "X-API-Key": "your-api-key"
    }
  }
}
```

### Startup.cs / Program.cs

```csharp
using Azure.Redis.Resilient.Client.Extensions;

// Configuración desde appsettings.json
builder.Services.AddResilientRedis(builder.Configuration);

// O configuración programática
builder.Services.AddResilientRedis(redis =>
{
    redis.HostName = "your-redis-cache.redis.cache.windows.net";
    redis.UseManagedIdentity = true;
    redis.KeyPrefix = "myapp";
}, serviceBus =>
{
    serviceBus.Namespace = "your-servicebus-namespace";
    serviceBus.EnableEventPublishing = true;
});
```

## Uso Básico

```csharp
public class UserService
{
    private readonly IResilientRedisClient _redisClient;

    public UserService(IResilientRedisClient redisClient)
    {
        _redisClient = redisClient;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        var key = $"user:{userId}";
        
        // Obtener con fallback automático
        var result = await _redisClient.GetOrCreateAsync(key, async () =>
        {
            // Este método se ejecuta solo si no está en cache
            return await _userRepository.GetByIdAsync(userId);
        }, TimeSpan.FromMinutes(30));

        return result.Value;
    }

    public async Task<bool> CacheUserAsync(User user)
    {
        var key = $"user:{user.Id}";
        var result = await _redisClient.SetAsync(key, user, TimeSpan.FromHours(1));
        
        return result.Success;
    }
}
```

## Uso Avanzado

### Operaciones con Resultados Detallados

```csharp
public async Task<ApiResponse<User>> GetUserWithMetricsAsync(int userId)
{
    var key = $"user:{userId}";
    var result = await _redisClient.GetAsync<User>(key);

    return new ApiResponse<User>
    {
        Data = result.Value,
        FromCache = result.FromCache,
        FromFallback = result.FromFallback,
        ExecutionTime = result.ExecutionTime,
        Success = result.Success
    };
}
```

### Invalidación por Patrón

```csharp
public async Task InvalidateUserCacheAsync(int userId)
{
    // Invalida todas las claves que coincidan con el patrón
    var result = await _redisClient.InvalidatePatternAsync($"user:{userId}:*");
    
    _logger.LogInformation("Invalidated {Count} cache entries", result.Value);
}
```

## Eventos de Service Bus

El cliente publica automáticamente eventos a Service Bus que puedes consumir:

```csharp
public class RedisEventHandler
{
    public async Task HandleRedisEvent(RedisEvent redisEvent)
    {
        switch (redisEvent.EventType)
        {
            case RedisEventType.CacheMiss:
                // Lógica para cache miss
                break;
            case RedisEventType.FallbackTriggered:
                // Lógica para cuando se usa fallback
                break;
            case RedisEventType.Error:
                // Lógica para errores
                break;
        }
    }
}
```

## Configuración de Managed Identity

### En Azure App Service

1. Habilita System Assigned Identity en tu App Service
2. Asigna el rol "Redis Cache Contributor" a la identidad
3. Configura `UseManagedIdentity: true`

### En desarrollo local

```bash
az login
# El cliente usará automáticamente tus credenciales de Azure CLI
```

## Mejores Prácticas

1. **Prefijos de Claves**: Usa prefijos consistentes para organizar tus datos
2. **Expiración**: Siempre establece tiempos de expiración apropiados
3. **Fallback**: Implementa servicios de fallback robustos
4. **Monitoring**: Monitorea los eventos de Service Bus para detectar problemas
5. **Testing**: Usa el patrón de inyección de dependencias para testing

## Troubleshooting

### Problemas de Conexión

- Verifica que Managed Identity esté habilitada
- Confirma que los roles de Azure estén asignados correctamente
- Revisa los logs para errores de autenticación

### Problemas de Rendimiento

- Ajusta los valores de timeout y retry
- Monitorea las métricas de Service Bus
- Considera usar connection pooling

## Contribuir

1. Fork el repositorio
2. Crea una rama para tu feature
3. Commit tus cambios
4. Push a la rama
5. Crea un Pull Request

## Licencia

MIT License - ver [LICENSE](LICENSE) para detalles.

## Hecho con amor por ju4r3v0l para mis amigos y la comunidad de desarrolladores. ❤️
