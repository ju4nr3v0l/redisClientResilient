# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-07-02

### Agregado
- ✅ Cliente Redis resiliente con reintentos automáticos usando Polly
- ✅ Soporte completo para Azure Managed Identity
- ✅ Integración con Azure Service Bus para publicación de eventos
- ✅ Sistema de fallback automático cuando Redis no está disponible
- ✅ Logging estructurado con diferentes niveles de detalle
- ✅ Configuración flexible a través de appsettings.json o código
- ✅ Operaciones básicas: Get, Set, Delete, Exists
- ✅ Operación avanzada: GetOrCreateAsync con fallback automático
- ✅ Invalidación de cache por patrones
- ✅ Métricas de rendimiento y tiempo de ejecución
- ✅ Soporte para prefijos de claves personalizables
- ✅ Manejo robusto de errores y excepciones
- ✅ Documentación completa con ejemplos
- ✅ Guía para desarrolladores
- ✅ Ejemplos de configuración y uso

### Características Técnicas
- **Target Framework**: .NET 8.0
- **Dependencias principales**:
  - StackExchange.Redis 2.7.33
  - Azure.Identity 1.12.1
  - Azure.Messaging.ServiceBus 7.17.5
  - Polly 8.2.0
  - System.Text.Json 8.0.5

### Patrones Implementados
- **Retry Pattern**: Reintentos con backoff exponencial
- **Circuit Breaker**: Prevención de llamadas cuando el servicio está caído
- **Timeout Pattern**: Timeouts configurables
- **Fallback Pattern**: Respaldo automático a servicios alternativos

### Eventos de Service Bus
- `CacheHit`: Cuando se encuentra un valor en cache
- `CacheMiss`: Cuando no se encuentra un valor en cache
- `FallbackTriggered`: Cuando se usa el servicio de fallback
- `Error`: Cuando ocurre un error en las operaciones
- `Set`: Cuando se guarda un valor en cache
- `Delete`: Cuando se elimina un valor del cache

### Configuración
- Soporte para connection strings tradicionales
- Soporte para Managed Identity en Azure
- Configuración de Service Bus con Managed Identity
- Configuración de servicios de fallback con headers personalizados
- Configuración de timeouts y reintentos

## [Unreleased]

### Planeado para futuras versiones
- [ ] Soporte para Redis Cluster
- [ ] Operaciones batch optimizadas
- [ ] Compresión automática de valores grandes
- [ ] Métricas de Application Insights integradas
- [ ] Soporte para múltiples instancias de Redis
- [ ] Cache warming automático
- [ ] Distributed locking
- [ ] Pub/Sub integration
