# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-01-02

### ✨ Added
- **Configuración opcional para Managed Identity**: Ahora `UseManagedIdentity` es `false` por defecto
- **Configuración opcional para Service Bus**: Ahora `EnableEventPublishing` es `false` por defecto
- **Método `AddResilientRedisBasic()`**: Para usar solo Redis sin Service Bus ni Fallback
- **Implementaciones NoOp**: `NoOpEventPublisher` y `NoOpFallbackService` para cuando las características están deshabilitadas
- **Soporte para Connection String**: Alternativa a Managed Identity para conexiones Redis
- **Ejemplos completos**: Carpeta `examples/` con patrones de uso comunes
- **Guía de patrones de fallback**: Documentación detallada en `FALLBACK_PATTERNS.md`

### 🔧 Changed
- **Namespace**: Cambiado de `Azure.Redis.Resilient.Client` a `ResilientRedis.Client` (namespace no reservado)
- **Package ID**: Cambiado de `Azure.Redis.Resilient.Client` a `ResilientRedis.Client`
- **Configuración por defecto**: Managed Identity y Service Bus ahora son opcionales (deshabilitados por defecto)
- **RedisOptions.ConnectionString**: Ahora es nullable para soportar configuración con Managed Identity

### 📚 Documentation
- **README actualizado**: Ejemplos de configuración flexible y patrones de fallback
- **Nuevos ejemplos**: BasicUsage, ManagedIdentityExample, MicroserviceFallback
- **Guía de patrones**: Documentación detallada de diferentes estrategias de fallback

### 🛠️ Technical
- **Inyección de dependencias mejorada**: Registro condicional de servicios basado en configuración
- **Mejor manejo de errores**: Validación de configuración mejorada
- **Scripts actualizados**: Scripts de publicación actualizados para nueva versión

## [1.0.0] - 2025-01-02

### ✨ Added
- **Cliente Redis resiliente**: Implementación base con Polly para reintentos y circuit breaker
- **Soporte para Managed Identity**: Autenticación segura con Azure Identity
- **Integración con Service Bus**: Publicación automática de eventos Redis
- **Servicio de Fallback**: Sistema de respaldo con HttpClient
- **Configuración flexible**: Soporte para appsettings.json y configuración programática
- **Logging estructurado**: Logging completo con diferentes niveles
- **Documentación XML**: Comentarios XML completos para IntelliSense
- **Tests unitarios**: Suite básica de pruebas con xUnit, Moq y FluentAssertions
- **Scripts de automatización**: Scripts para validación, testing y publicación

### 🏗️ Architecture
- **Patrón Repository**: Interfaces bien definidas para extensibilidad
- **Dependency Injection**: Integración completa con Microsoft.Extensions.DependencyInjection
- **Configuration Pattern**: Uso de IOptions para configuración tipada
- **Event-Driven**: Publicación de eventos para monitoreo y auditoría

### 📦 Package
- **NuGet Package**: Configuración completa para publicación en NuGet.org
- **Symbols Package**: Soporte para debugging con símbolos
- **Multi-targeting**: Soporte para .NET 8.0
- **Dependencies**: Dependencias mínimas y bien versionadas

---

## Tipos de Cambios

- `Added` para nuevas características
- `Changed` para cambios en funcionalidad existente
- `Deprecated` para características que serán removidas
- `Removed` para características removidas
- `Fixed` para corrección de bugs
- `Security` para vulnerabilidades de seguridad
