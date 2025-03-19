using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using backend.Models;

namespace backend.Services
{
    /// <summary>
    /// Defines the contract for flight-related operations, focusing on airport searches.
    /// </summary>
    public interface IFlightService
    {
        /// <summary>
        /// Searches for airports and cities based on a query using the Sky Scrapper API for autocomplete purposes.
        /// </summary>
        /// <param name="query">The search term (e.g., "new" for New York airports).</param>
        /// <returns>A JSON string containing an API response with a list of matching entities for autocomplete, or an error message.</returns>
        Task<string> SearchAirportAsync(string query);
    }

    /// <summary>
    /// Implements flight-related services, including searching airports via the Sky Scrapper API.
    /// </summary>
    public class FlightService : IFlightService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FlightService> _logger;

        public FlightService(HttpClient httpClient, IConfiguration configuration, ILogger<FlightService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Headers are already set in AddHttpClient, so no need to set them here
        }

        public async Task<string> SearchAirportAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("SearchAirportAsync called with empty or null query.");
                return JsonSerializer.Serialize(new ApiResponse<List<AutocompleteResult>>(success: false, data: null, error: "Query cannot be empty."));
            }

            try
            {
                _logger.LogInformation("Searching airports for query: {Query}", query);

                var requestUrl = $"api/v1/flights/searchAirport?query={Uri.EscapeDataString(query)}&locale=en-GB";
                var response = await _httpClient.GetAsync(requestUrl);

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Sky Scrapper API response for query {Query}: {Content}", query, content);

                using var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("status", out var statusElement) || !statusElement.GetBoolean())
                {
                    _logger.LogWarning("Invalid response status for query: {Query}", query);
                    return JsonSerializer.Serialize(new ApiResponse<List<AutocompleteResult>>(success: false, data: null, error: "Invalid response from API."));
                }

                if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("No airports found for query: {Query}", query);
                    return JsonSerializer.Serialize(new ApiResponse<List<AutocompleteResult>>(success: true, data: new List<AutocompleteResult>()));
                }

                var autocompleteResults = new List<AutocompleteResult>();
                foreach (var item in dataElement.EnumerateArray())
                {
                    var presentation = item.GetProperty("presentation");
                    var navigation = item.GetProperty("navigation");
                    var flightParams = navigation.GetProperty("relevantFlightParams");

                    var suggestionTitle = presentation.GetProperty("suggestionTitle").GetString();
                    var subtitle = presentation.GetProperty("subtitle").GetString();
                    var skyId = flightParams.GetProperty("skyId").GetString();
                    var entityId = navigation.GetProperty("entityId").GetString();
                    var entityType = navigation.GetProperty("entityType").GetString();
                    var localizedName = navigation.GetProperty("localizedName").GetString();

                    autocompleteResults.Add(new AutocompleteResult
                    {
                        Label = suggestionTitle,
                        Subtitle = subtitle,
                        Value = new AutocompleteValue
                        {
                            SkyId = skyId,
                            EntityId = entityId,
                            EntityType = entityType,
                            LocalizedName = localizedName
                        }
                    });
                }

                return JsonSerializer.Serialize(new ApiResponse<List<AutocompleteResult>>(success: true, data: autocompleteResults));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch airports for query {Query} due to network or API error.", query);
                return JsonSerializer.Serialize(new ApiResponse<List<AutocompleteResult>>(success: false, data: null, error: "Failed to connect to Sky Scrapper API."));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Sky Scrapper API response for query {Query}.", query);
                return JsonSerializer.Serialize(new ApiResponse<List<AutocompleteResult>>(success: false, data: null, error: "Invalid API response format."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while searching airports for query {Query}.", query);
                return JsonSerializer.Serialize(new ApiResponse<List<AutocompleteResult>>(success: false, data: null, error: "An unexpected error occurred."));
            }
        }
    }
}