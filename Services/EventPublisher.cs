using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Redis.Resilient.Client.Configuration;
using Azure.Redis.Resilient.Client.Interfaces;
using Azure.Redis.Resilient.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Azure.Redis.Resilient.Client.Services;

public class EventPublisher : IEventPublisher, IDisposable
{
    private readonly ServiceBusClient? _serviceBusClient;
    private readonly ServiceBusSender? _sender;
    private readonly ILogger<EventPublisher> _logger;
    private readonly ServiceBusOptions _options;
    private bool _disposed;

    public EventPublisher(IOptions<ServiceBusOptions> options, ILogger<EventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!_options.EnableEventPublishing)
        {
            _logger.LogInformation("Event publishing is disabled");
            return;
        }

        try
        {
            if (_options.UseManagedIdentity && !string.IsNullOrEmpty(_options.Namespace))
            {
                var credential = new DefaultAzureCredential();
                _serviceBusClient = new ServiceBusClient($"{_options.Namespace}.servicebus.windows.net", credential);
            }
            else if (!string.IsNullOrEmpty(_options.ConnectionString))
            {
                _serviceBusClient = new ServiceBusClient(_options.ConnectionString);
            }
            else
            {
                throw new InvalidOperationException("Service Bus configuration is invalid");
            }

            _sender = _serviceBusClient.CreateSender(_options.RedisEventsTopic);
            _logger.LogInformation("Event publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Event Publisher");
            throw;
        }
    }

    public async Task PublishAsync(RedisEvent redisEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableEventPublishing || _sender == null)
        {
            _logger.LogDebug("Event publishing is disabled or sender is not initialized");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(redisEvent);
            var message = new ServiceBusMessage(json)
            {
                MessageId = redisEvent.EventId,
                Subject = redisEvent.EventType.ToString(),
                ContentType = "application/json"
            };

            // Agregar propiedades para filtrado
            message.ApplicationProperties["EventType"] = redisEvent.EventType.ToString();
            message.ApplicationProperties["Key"] = redisEvent.Key;
            message.ApplicationProperties["Success"] = redisEvent.Success;
            message.ApplicationProperties["FallbackUsed"] = redisEvent.FallbackUsed;

            await _sender.SendMessageAsync(message, cancellationToken);
            
            _logger.LogDebug("Published Redis event {EventId} of type {EventType}", 
                redisEvent.EventId, redisEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Redis event {EventId}", redisEvent.EventId);
            // No re-lanzamos la excepción para no afectar la operación principal
        }
    }

    public async Task PublishBatchAsync(IEnumerable<RedisEvent> events, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableEventPublishing || _sender == null)
        {
            return;
        }

        var eventList = events.ToList();
        if (!eventList.Any())
        {
            return;
        }

        try
        {
            var messages = eventList.Select(evt =>
            {
                var json = JsonSerializer.Serialize(evt);
                var message = new ServiceBusMessage(json)
                {
                    MessageId = evt.EventId,
                    Subject = evt.EventType.ToString(),
                    ContentType = "application/json"
                };

                message.ApplicationProperties["EventType"] = evt.EventType.ToString();
                message.ApplicationProperties["Key"] = evt.Key;
                message.ApplicationProperties["Success"] = evt.Success;
                message.ApplicationProperties["FallbackUsed"] = evt.FallbackUsed;

                return message;
            }).ToList();

            await _sender.SendMessagesAsync(messages, cancellationToken);
            
            _logger.LogDebug("Published batch of {Count} Redis events", eventList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch of Redis events");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _sender?.DisposeAsync().AsTask().Wait();
        _serviceBusClient?.DisposeAsync().AsTask().Wait();
        _disposed = true;
    }
}
