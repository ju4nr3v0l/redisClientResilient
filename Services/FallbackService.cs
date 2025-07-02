using ResilientRedis.Client.Configuration;
using ResilientRedis.Client.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ResilientRedis.Client.Services;

/// <summary>
/// Service for handling fallback operations when Redis is unavailable
/// </summary>
public class FallbackService : IFallbackService
{
    private readonly HttpClient _httpClient;
    private readonly FallbackServiceOptions _options;
    private readonly ILogger<FallbackService> _logger;

    /// <summary>
    /// Initializes a new instance of the FallbackService class
    /// </summary>
    /// <param name="httpClient">HTTP client for external API calls</param>
    /// <param name="options">Fallback service configuration options</param>
    /// <param name="logger">Logger instance</param>
    public FallbackService(HttpClient httpClient, IOptions<FallbackServiceOptions> options, ILogger<FallbackService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Configurar HttpClient
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        // Agregar headers adicionales
        foreach (var header in _options.Headers)
        {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Retrieves data from the fallback service
    /// </summary>
    /// <typeparam name="T">Type of data to retrieve</typeparam>
    /// <param name="key">Key identifier for the data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Retrieved data or default value</returns>
    public async Task<T?> GetDataAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching data from fallback service for key: {Key}", key);

            var response = await _httpClient.GetAsync($"api/cache/{Uri.EscapeDataString(key)}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(content);
                
                _logger.LogDebug("Successfully retrieved data from fallback service for key: {Key}", key);
                return result;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Data not found in fallback service for key: {Key}", key);
                return default;
            }

            _logger.LogWarning("Fallback service returned status {StatusCode} for key: {Key}", 
                response.StatusCode, key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from fallback service for key: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Creates data in the fallback service
    /// </summary>
    /// <typeparam name="T">Type of data to create</typeparam>
    /// <param name="key">Key identifier for the data</param>
    /// <param name="parameters">Parameters for data creation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created data or default value</returns>
    public async Task<T?> CreateDataAsync<T>(string key, object parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating data in fallback service for key: {Key}", key);

            var requestBody = new
            {
                Key = key,
                Parameters = parameters
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/cache", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(responseContent);
                
                _logger.LogDebug("Successfully created data in fallback service for key: {Key}", key);
                return result;
            }

            _logger.LogWarning("Fallback service returned status {StatusCode} when creating data for key: {Key}", 
                response.StatusCode, key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating data in fallback service for key: {Key}", key);
            return default;
        }
    }
}
