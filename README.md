# Resilient Redis Client

Un cliente resiliente para Azure Cache for Redis con soporte **opcional** para Managed Identity, Service Bus events y patrones de fallback.

## Características

- ✅ **Resiliencia**: Reintentos automáticos con backoff exponencial
- ✅ **Managed Identity**: Autenticación segura sin connection strings (opcional)
- ✅ **Service Bus Integration**: Publicación automática de eventos (opcional)
- ✅ **Fallback Service**: Sistema de respaldo cuando Redis no está disponible (opcional)
- ✅ **Logging**: Logging estructurado con diferentes niveles
- ✅ **Métricas**: Seguimiento de rendimiento y eventos
- ✅ **Configuración flexible**: Múltiples formas de configuración

## Instalación

```bash
dotnet add package ResilientRedis.Client
```

## Configuraciones Disponibles

### 🔧 **Configuración Básica (Solo Redis)**

Para usar solo Redis sin Service Bus ni Fallback:

```csharp
// Program.cs
builder.Services.AddResilientRedisBasic(redis =>
{
    redis.ConnectionString = "your-redis-connection-string";
    redis.KeyPrefix = "myapp";
});
```

### 🔧 **Configuración con Managed Identity**

```csharp
builder.Services.AddResilientRedis(redis =>
{
    redis.HostName = "your-redis-cache.redis.cache.windows.net";
    redis.UseManagedIdentity = true;
    redis.KeyPrefix = "myapp";
});
```

### 🔧 **Configuración Completa con Service Bus**

```csharp
builder.Services.AddResilientRedis(
    redis =>
    {
        redis.HostName = "your-redis-cache.redis.cache.windows.net";
        redis.UseManagedIdentity = true;
        redis.KeyPrefix = "myapp";
    },
    serviceBus =>
    {
        serviceBus.Namespace = "your-servicebus-namespace";
        serviceBus.EnableEventPublishing = true;
        serviceBus.UseManagedIdentity = true;
    },
    fallback =>
    {
        fallback.BaseUrl = "https://your-api.azurewebsites.net";
        fallback.TimeoutSeconds = 30;
    });
```

### 🔧 **Configuración desde appsettings.json**

```json
{
  "Redis": {
    "ConnectionString": "your-redis-connection-string",
    // O para Managed Identity:
    "HostName": "your-redis-cache.redis.cache.windows.net",
    "UseManagedIdentity": true,
    "KeyPrefix": "myapp",
    "DefaultExpirationMinutes": 60
  },
  "ServiceBus": {
    "EnableEventPublishing": true,
    "Namespace": "your-servicebus-namespace",
    "UseManagedIdentity": true,
    "RedisEventsTopic": "redis-events"
  },
  "FallbackService": {
    "BaseUrl": "https://your-api.azurewebsites.net",
    "TimeoutSeconds": 30,
    "Headers": {
      "X-API-Key": "your-api-key"
    }
  }
}
```

```csharp
// Program.cs
builder.Services.AddResilientRedis(builder.Configuration);
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
}
```

## Patrones de Fallback

### 🔄 **Patrón 1: Fallback a Base de Datos**

```csharp
public class ProductService
{
    private readonly IResilientRedisClient _redisClient;
    private readonly IProductRepository _repository;

    public async Task<Product?> GetProductAsync(int productId)
    {
        var key = $"product:{productId}";
        
        var result = await _redisClient.GetOrCreateAsync(key, async () =>
        {
            // Fallback: obtener de base de datos
            var product = await _repository.GetByIdAsync(productId);
            return product;
        }, TimeSpan.FromHours(1));

        return result.Value;
    }
}
```

### 🔄 **Patrón 2: Fallback a Microservicio Externo**

```csharp
public class InventoryService
{
    private readonly IResilientRedisClient _redisClient;
    private readonly HttpClient _httpClient;

    public async Task<InventoryItem?> GetInventoryAsync(string sku)
    {
        var key = $"inventory:{sku}";
        
        var result = await _redisClient.GetOrCreateAsync(key, async () =>
        {
            // Fallback: llamar a microservicio de inventario
            var response = await _httpClient.GetAsync($"/api/inventory/{sku}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<InventoryItem>(json);
            }
            return null;
        }, TimeSpan.FromMinutes(15));

        return result.Value;
    }
}
```

### 🔄 **Patrón 3: Microservicio Responsable de Cache**

Cuando otro microservicio es responsable de poblar el cache:

```csharp
public class OrderService
{
    private readonly IResilientRedisClient _redisClient;
    private readonly IOrderProcessingService _orderProcessor;

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        var key = $"order:{orderId}";
        
        // Intentar obtener del cache
        var cachedResult = await _redisClient.GetAsync<Order>(key);
        
        if (cachedResult.Success && cachedResult.Value != null)
        {
            return cachedResult.Value;
        }

        // Si no está en cache, solicitar al microservicio responsable
        await _orderProcessor.RequestOrderCachingAsync(orderId);
        
        // Esperar un momento y reintentar
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        
        var retryResult = await _redisClient.GetAsync<Order>(key);
        return retryResult.Value;
    }
}

// Microservicio responsable del cache
public class OrderProcessingService : IOrderProcessingService
{
    private readonly IResilientRedisClient _redisClient;
    private readonly IOrderRepository _repository;

    public async Task RequestOrderCachingAsync(int orderId)
    {
        // Obtener datos y cachear
        var order = await _repository.GetOrderWithDetailsAsync(orderId);
        if (order != null)
        {
            var key = $"order:{orderId}";
            await _redisClient.SetAsync(key, order, TimeSpan.FromHours(2));
        }
    }
}
```

### 🔄 **Patrón 4: Cache-Aside con Notificación**

```csharp
public class CatalogService
{
    private readonly IResilientRedisClient _redisClient;
    private readonly ICatalogRepository _repository;
    private readonly IServiceBus _serviceBus;

    public async Task<Product?> GetProductAsync(int productId)
    {
        var key = $"product:{productId}";
        
        var result = await _redisClient.GetAsync<Product>(key);
        
        if (!result.Success || result.Value == null)
        {
            // Notificar que se necesita el producto en cache
            await _serviceBus.PublishAsync(new ProductCacheRequest 
            { 
                ProductId = productId,
                RequestedBy = "CatalogService",
                Timestamp = DateTime.UtcNow
            });
            
            // Obtener directamente de la base de datos como fallback
            return await _repository.GetByIdAsync(productId);
        }

        return result.Value;
    }
}
```

## Configuración de Managed Identity

### En Azure App Service

1. Habilita System Assigned Identity en tu App Service
2. Asigna los roles necesarios:
   - **Redis**: "Redis Cache Contributor"
   - **Service Bus**: "Azure Service Bus Data Sender"

### En desarrollo local

```bash
az login
# El cliente usará automáticamente tus credenciales de Azure CLI
```

## Monitoreo y Eventos

Si tienes Service Bus habilitado, puedes monitorear eventos:

```csharp
public class RedisEventHandler
{
    public async Task HandleRedisEvent(RedisEvent redisEvent)
    {
        switch (redisEvent.EventType)
        {
            case RedisEventType.CacheMiss:
                // Lógica para cache miss
                _metrics.IncrementCounter("redis.cache_miss");
                break;
            case RedisEventType.FallbackTriggered:
                // Lógica para cuando se usa fallback
                _metrics.IncrementCounter("redis.fallback_used");
                break;
            case RedisEventType.Error:
                // Lógica para errores
                _logger.LogError("Redis error: {Message}", redisEvent.ErrorMessage);
                break;
        }
    }
}
```

## Mejores Prácticas

### 🎯 **Para Configuración**
- Usa **configuración básica** si solo necesitas Redis
- Habilita **Service Bus** solo si necesitas monitoreo de eventos
- Configura **Managed Identity** en producción para mayor seguridad

### 🎯 **Para Fallback**
- Implementa timeouts apropiados en servicios de fallback
- Considera el impacto en rendimiento de llamadas externas
- Usa circuit breakers para servicios externos inestables

### 🎯 **Para Microservicios**
- Define claramente qué servicio es responsable de cada cache
- Implementa patrones de notificación para cache warming
- Usa TTL apropiados según la frecuencia de cambio de datos

## Troubleshooting

### Problemas de Conexión
- Verifica connection strings o configuración de Managed Identity
- Confirma que los roles de Azure estén asignados correctamente
- Revisa los logs para errores de autenticación

### Problemas de Rendimiento
- Ajusta los valores de timeout y retry
- Monitorea las métricas de Service Bus (si está habilitado)
- Considera usar connection pooling

## Ejemplos Completos

Ver la carpeta `examples/` para implementaciones completas de diferentes patrones.

## Contribuir

1. Fork el repositorio
2. Crea una rama para tu feature
3. Commit tus cambios
4. Push a la rama
5. Crea un Pull Request

## Licencia

MIT License - ver [LICENSE](LICENSE) para detalles.

## Hecho con ❤️ por ju4r3v0l para la comunidad de desarrolladores
