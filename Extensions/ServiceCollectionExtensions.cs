using Azure.Identity;
using Azure.Redis.Resilient.Client.Configuration;
using Azure.Redis.Resilient.Client.Interfaces;
using Azure.Redis.Resilient.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Azure.Redis.Resilient.Client.Extensions;

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

        // Registrar ConnectionMultiplexer como singleton
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
            var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                ?? throw new InvalidOperationException("Redis configuration is missing");

            try
            {
                ConfigurationOptions configOptions;

                if (!string.IsNullOrEmpty(redisOptions.ConnectionString))
                {
                    configOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
                }
                else
                {
                    configOptions = new ConfigurationOptions
                    {
                        EndPoints = { $"{redisOptions.HostName}:{redisOptions.Port}" },
                        Ssl = redisOptions.UseSsl,
                        AbortOnConnectFail = false,
                        ConnectRetry = 3,
                        ConnectTimeout = 10000,
                        SyncTimeout = 5000
                    };

                    if (redisOptions.UseManagedIdentity)
                    {
                        var credential = new DefaultAzureCredential();
                        configOptions.ConfigurationChannel = "__keyspace@0__:*";
                        // Para Azure Cache for Redis con Managed Identity
                        configOptions.User = redisOptions.HostName.Split('.')[0];
                    }
                }

                var multiplexer = ConnectionMultiplexer.Connect(configOptions);
                
                // Configurar eventos de conexión
                multiplexer.ConnectionFailed += (sender, args) =>
                {
                    logger.LogError("Redis connection failed: {Exception}", args.Exception?.Message);
                };

                multiplexer.ConnectionRestored += (sender, args) =>
                {
                    logger.LogInformation("Redis connection restored");
                };

                logger.LogInformation("Redis connection established successfully");
                return multiplexer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to establish Redis connection");
                throw;
            }
        });

        // Registrar HttpClient para FallbackService
        services.AddHttpClient<IFallbackService, FallbackService>();

        // Registrar servicios
        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddScoped<IFallbackService, FallbackService>();
        services.AddScoped<IResilientRedisClient, ResilientRedisClient>();

        return services;
    }

    /// <summary>
    /// Registra el cliente resiliente de Redis con configuración personalizada
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

        // Registrar ConnectionMultiplexer
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
            var redisOptions = new RedisOptions();
            configureRedis(redisOptions);

            try
            {
                ConfigurationOptions configOptions;

                if (!string.IsNullOrEmpty(redisOptions.ConnectionString))
                {
                    configOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
                }
                else
                {
                    configOptions = new ConfigurationOptions
                    {
                        EndPoints = { $"{redisOptions.HostName}:{redisOptions.Port}" },
                        Ssl = redisOptions.UseSsl,
                        AbortOnConnectFail = false,
                        ConnectRetry = 3,
                        ConnectTimeout = 10000,
                        SyncTimeout = 5000
                    };

                    if (redisOptions.UseManagedIdentity)
                    {
                        var credential = new DefaultAzureCredential();
                        configOptions.User = redisOptions.HostName.Split('.')[0];
                    }
                }

                var multiplexer = ConnectionMultiplexer.Connect(configOptions);
                
                multiplexer.ConnectionFailed += (sender, args) =>
                {
                    logger.LogError("Redis connection failed: {Exception}", args.Exception?.Message);
                };

                multiplexer.ConnectionRestored += (sender, args) =>
                {
                    logger.LogInformation("Redis connection restored");
                };

                logger.LogInformation("Redis connection established successfully");
                return multiplexer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to establish Redis connection");
                throw;
            }
        });

        services.AddHttpClient<IFallbackService, FallbackService>();
        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddScoped<IFallbackService, FallbackService>();
        services.AddScoped<IResilientRedisClient, ResilientRedisClient>();

        return services;
    }
}
