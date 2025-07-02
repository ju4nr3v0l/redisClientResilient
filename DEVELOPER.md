# Guía para Desarrolladores - Azure Redis Resilient Client

## Arquitectura del Cliente

### Componentes Principales

1. **IResilientRedisClient**: Interfaz principal que expone todas las operaciones de cache
2. **ResilientRedisClient**: Implementación principal con lógica de resiliencia
3. **IEventPublisher**: Maneja la publicación de eventos a Service Bus
4. **IFallbackService**: Servicio de respaldo cuando Redis no está disponible

### Flujo de Operaciones

```
Cliente → ResilientRedisClient → Redis
                ↓ (si falla)
            FallbackService → API Externa
                ↓
            EventPublisher → Service Bus
```

## Patrones de Resiliencia Implementados

### 1. Retry Pattern
- Reintentos automáticos con backoff exponencial
- Configurable a través de `MaxRetryAttempts` y `RetryDelaySeconds`
- Usa la librería Polly para manejo robusto

### 2. Circuit Breaker
- Implementado a través de Polly
- Previene llamadas innecesarias cuando el servicio está caído

### 3. Timeout Pattern
- Timeouts configurables para evitar bloqueos
- Aplicado tanto a Redis como al servicio de fallback

### 4. Fallback Pattern
- Cuando Redis falla, automáticamente usa el servicio de fallback
- Los datos obtenidos del fallback se guardan en Redis para futuras consultas

## Configuración Avanzada

### Managed Identity Setup

Para usar Managed Identity en Azure:

```csharp
// En Azure App Service
services.AddResilientRedis(redis =>
{
    redis.HostName = "your-cache.redis.cache.windows.net";
    redis.UseManagedIdentity = true;
    redis.UseSsl = true;
});
```

### Service Bus Events

Los eventos se publican automáticamente con la siguiente estructura:

```json
{
  "eventId": "guid",
  "eventType": "CacheHit|CacheMiss|Error|FallbackTriggered",
  "key": "cache-key",
  "success": true,
  "timestamp": "2024-01-01T00:00:00Z",
  "executionTimeMs": 150,
  "fallbackUsed": false,
  "errorMessage": null
}
```

### Personalización del Fallback Service

Implementa tu propio `IFallbackService`:

```csharp
public class CustomFallbackService : IFallbackService
{
    public async Task<T?> GetDataAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Tu lógica personalizada aquí
        return default;
    }

    public async Task<T?> CreateDataAsync<T>(string key, object parameters, CancellationToken cancellationToken = default)
    {
        // Tu lógica personalizada aquí
        return default;
    }
}

// Registrar en DI
services.AddScoped<IFallbackService, CustomFallbackService>();
```

## Testing

### Unit Testing

```csharp
[Test]
public async Task GetAsync_WhenRedisIsDown_ShouldUseFallback()
{
    // Arrange
    var mockRedis = new Mock<IConnectionMultiplexer>();
    var mockFallback = new Mock<IFallbackService>();
    var mockEventPublisher = new Mock<IEventPublisher>();
    
    mockFallback.Setup(x => x.GetDataAsync<string>("test-key", default))
               .ReturnsAsync("fallback-value");

    var client = new ResilientRedisClient(
        mockRedis.Object,
        mockFallback.Object,
        mockEventPublisher.Object,
        Options.Create(new RedisOptions()),
        Mock.Of<ILogger<ResilientRedisClient>>());

    // Act
    var result = await client.GetOrCreateAsync("test-key", 
        () => Task.FromResult("fallback-value"));

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.FromFallback, Is.True);
    Assert.That(result.Value, Is.EqualTo("fallback-value"));
}
```

### Integration Testing

```csharp
[Test]
public async Task IntegrationTest_WithRealRedis()
{
    // Usar TestContainers para Redis
    var redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    await redis.StartAsync();

    var connectionString = redis.GetConnectionString();
    
    // Configurar cliente con Redis real
    var services = new ServiceCollection();
    services.AddResilientRedis(options =>
    {
        options.ConnectionString = connectionString;
        options.UseManagedIdentity = false;
    });

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<IResilientRedisClient>();

    // Ejecutar pruebas...
}
```

## Métricas y Monitoring

### Eventos Importantes a Monitorear

1. **Cache Hit Ratio**: Porcentaje de hits vs misses
2. **Fallback Usage**: Frecuencia de uso del servicio de fallback
3. **Error Rate**: Tasa de errores en operaciones
4. **Response Time**: Tiempo de respuesta promedio

### Configuración de Application Insights

```csharp
services.AddApplicationInsightsTelemetry();

// Los eventos se enviarán automáticamente a Application Insights
```

## Mejores Prácticas

### 1. Naming Conventions
- Usa prefijos consistentes para las claves
- Incluye versioning en las claves cuando sea necesario
- Ejemplo: `myapp:v1:user:123`

### 2. Serialización
- El cliente usa System.Text.Json por defecto
- Para objetos complejos, considera usar compresión
- Evita serializar objetos muy grandes

### 3. Expiración
- Siempre establece tiempos de expiración
- Usa diferentes TTL según el tipo de datos
- Considera el patrón de acceso a los datos

### 4. Error Handling
- Los errores se logean automáticamente
- Los eventos de error se publican a Service Bus
- Implementa alertas basadas en estos eventos

### 5. Performance
- Usa operaciones batch cuando sea posible
- Considera usar pipelines para múltiples operaciones
- Monitorea el uso de memoria de Redis

## Troubleshooting

### Problemas Comunes

1. **Connection Timeout**
   - Verificar configuración de red
   - Revisar configuración de SSL
   - Comprobar Managed Identity

2. **Serialization Errors**
   - Verificar que los objetos sean serializables
   - Revisar configuración de JsonSerializer

3. **Service Bus Errors**
   - Verificar permisos de Managed Identity
   - Comprobar configuración del tópico

### Logs Importantes

```
[Information] Redis connection established successfully
[Warning] Redis connection failed: {Exception}
[Error] Failed to publish Redis event {EventId}
[Debug] Cache hit for key: {Key}
[Debug] Using fallback for key: {Key}
```

## Contribuir al Proyecto

1. Fork el repositorio
2. Crea una rama para tu feature: `git checkout -b feature/nueva-funcionalidad`
3. Implementa los cambios con tests
4. Ejecuta todos los tests: `dotnet test`
5. Crea un Pull Request

### Estándares de Código

- Usa C# 12 features cuando sea apropiado
- Sigue las convenciones de naming de .NET
- Incluye XML documentation para APIs públicas
- Mantén cobertura de tests > 80%
