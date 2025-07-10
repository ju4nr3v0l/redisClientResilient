using Azure.Identity;
using ResilientRedis.Client.Configuration;
using ResilientRedis.Client.Interfaces;
using ResilientRedis.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RedisNuget.Services;

namespace ResilientRedis.Client.Extensions;

/// <summary>
/// Extension methods for configuring Redis resilient client services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra el cliente resiliente de Redis con todas sus dependencias
    /// </summary>
    public static IServiceCollection AddResilientRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar opciones
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<FallbackServiceOptions>(configuration.GetSection(FallbackServiceOptions.SectionName));

        return AddResilientRedisCore(services);
    }

    /// <summary>
    /// Registra el cliente resiliente de Redis con configuración programática
    /// </summary>
    public static IServiceCollection AddResilientRedis(
        this IServiceCollection services,
        Action<RedisOptions> configureRedis,
        Action<ServiceBusOptions>? configureServiceBus = null,
        Action<FallbackServiceOptions>? configureFallback = null)
    {
        services.Configure(configureRedis);
        
        if (configureServiceBus != null)
            services.Configure(configureServiceBus);
        
        if (configureFallback != null)
            services.Configure(configureFallback);

        return AddResilientRedisCore(services);
    }

    /// <summary>
    /// Registra solo el cliente Redis básico sin Service Bus ni Fallback
    /// </summary>
    public static IServiceCollection AddResilientRedisBasic(
        this IServiceCollection services,
        Action<RedisOptions> configureRedis)
    {
        services.Configure(configureRedis);
        
        // Solo registrar Redis
        services.AddSingleton<IConnectionMultiplexer>(CreateRedisConnection);
        services.AddScoped<IResilientRedisClient, ResilientRedisClient>();
        
        // Registrar servicios dummy para dependencias opcionales
        services.AddScoped<IEventPublisher, NoOpEventPublisher>();
        services.AddScoped<IFallbackService, NoOpFallbackService>();
        
        return services;
    }

    private static IServiceCollection AddResilientRedisCore(IServiceCollection services)
    {
        // Registrar ConnectionMultiplexer como singleton
        services.AddSingleton<IConnectionMultiplexer>(CreateRedisConnection);

        // Registrar HttpClient para FallbackService
        services.AddHttpClient<IFallbackService, FallbackService>();

        // Registrar servicios condicionalmente
        services.AddScoped<IEventPublisher>(provider =>
        {
            var serviceBusOptions = provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
            if (serviceBusOptions.EnableEventPublishing)
            {
                return new EventPublisher(
                    provider.GetRequiredService<IOptions<ServiceBusOptions>>(),
                    provider.GetRequiredService<ILogger<EventPublisher>>());
            }
            return new NoOpEventPublisher();
        });

        services.AddScoped<IFallbackService, FallbackService>();
        services.AddScoped<IResilientRedisClient, ResilientRedisClient>();

        return services;
    }

    private static IConnectionMultiplexer CreateRedisConnection(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
        var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        try
        {
            ConfigurationOptions configOptions;

            if (!string.IsNullOrEmpty(redisOptions.ConnectionString))
            {
                // Usar connection string directamente
                configOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
                logger.LogInformation("Connecting to Redis using connection string");
            }
            else if (redisOptions.UseManagedIdentity)
            {
                // Usar Managed Identity
                configOptions = new ConfigurationOptions
                {
                    EndPoints = { $"{redisOptions.HostName}:{redisOptions.Port}" },
                    Ssl = redisOptions.UseSsl,
                    AbortOnConnectFail = false,
                };

                // Configurar autenticación con Managed Identity
                configOptions.ConfigurationChannel = "__keyspace@0__:*";
                logger.LogInformation("Connecting to Redis using Managed Identity for {HostName}:{Port}", 
                    redisOptions.HostName, redisOptions.Port);
            }
            else
            {
                throw new InvalidOperationException(
                    "Either ConnectionString or HostName with UseManagedIdentity must be configured");
            }

            var multiplexer = ConnectionMultiplexer.Connect(configOptions);
            
            // Configurar eventos de conexión
            multiplexer.ConnectionFailed += (sender, args) =>
                logger.LogError("Redis connection failed: {Exception}", args.Exception);
            
            multiplexer.ConnectionRestored += (sender, args) =>
                logger.LogInformation("Redis connection restored");

            logger.LogInformation("Redis connection established successfully");
            return multiplexer;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to Redis");
            throw;
        }
    }
}
