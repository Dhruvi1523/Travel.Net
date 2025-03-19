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
        Task<string> SearchFlightsAsync(SearchFlightsRequest request);
    }

    /// <summary>
    /// Implements flight-related services, including searching airports via the Sky Scrapper API.
    /// </summary>
    public class FlightService : IFlightService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FlightService> _logger;
        private readonly string CountryCode = "IN";
        private readonly string Market = "en-GB";
        private readonly string Currency = "INR";
        private readonly string Locale = "en-GB";


        /// <summary>
        /// Initializes a new instance of the <see cref="FlightService"/> class with the specified dependencies.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make requests to the Sky Scrapper API.</param>
        /// <param name="configuration">The configuration provider for accessing application settings.</param>
        /// <param name="logger">The logger for logging information, warnings, and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/>, <paramref name="configuration"/>, or <paramref name="logger"/> is null.</exception>
        public FlightService(HttpClient httpClient, IConfiguration configuration, ILogger<FlightService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        /// <summary>
        /// Asynchronously searches for airports and cities using the Sky Scrapper API based on the provided query.
        /// </summary>
        /// <param name="query">The search term to query the Sky Scrapper API (e.g., "mumbai" for Mumbai airports).</param>
        /// <returns>
        /// A JSON string representing an <see cref="ApiResponse{T}"/> containing a list of <see cref="AutocompleteResult"/> objects if successful,
        /// or an error message if the request fails.
        /// </returns>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to the Sky Scrapper API fails.</exception>
        /// <exception cref="JsonException">Thrown when the API response cannot be parsed as JSON.</exception>
        /// <exception cref="Exception">Thrown for unexpected errors during the search operation.</exception>
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

                var requestUrl = $"api/v1/flights/searchAirport?query={Uri.EscapeDataString(query)}&locale={Locale}";
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

        /// <summary>
        /// Formats the duration in minutes to a user-friendly string (e.g., "2h 55m").
        /// </summary>
        /// <param name="minutes">The duration in minutes.</param>
        /// <returns>A formatted duration string.</returns>
        private string FormatDuration(int minutes)
        {
            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;
            return $"{hours}h {remainingMinutes}m";
        }

        /// <summary>
        /// Asynchronously searches for flights using the Sky Scrapper API based on the provided search criteria.
        /// </summary>
        /// <param name="request">The search criteria for flights.</param>
        /// <returns>
        /// A JSON string representing an <see cref="ApiResponse{T}"/> containing a list of <see cref="FlightSearchResult"/> objects if successful,
        /// or an error message if the request fails.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        public async Task<string> SearchFlightsAsync(SearchFlightsRequest request)
        {

            if (request == null)
            {
                _logger.LogWarning("SearchFlightsAsync called with null request.");
                return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: false, data: null, error: "Request cannot be null."));
            }

            try
            {
                _logger.LogInformation("Searching flights with criteria: {OriginSkyId} to {DestinationSkyId} on {Date}", request.OriginSkyId, request.DestinationSkyId, request.Date);

                var queryParams = new List<string>
{
    $"originSkyId={Uri.EscapeDataString(request.OriginSkyId)}",
    $"destinationSkyId={Uri.EscapeDataString(request.DestinationSkyId)}",
    $"originEntityId={Uri.EscapeDataString(request.OriginEntityId)}",
    $"destinationEntityId={Uri.EscapeDataString(request.DestinationEntityId)}",
    $"date={Uri.EscapeDataString(request.Date)}",
    $"cabinClass={Uri.EscapeDataString(request.CabinClass.ToLower())}", // Ensuring lowercase if API expects it
    $"adults={request.Adults}",
    $"childrens={request.Children}",
    $"infants={request.Infants}",
    $"sortBy={Uri.EscapeDataString(request.SortBy.ToLower())}", // Ensuring lowercase if API expects it
    $"limit={request.Limit}",
    $"currency={Currency}",
    $"market={Market}",
    $"countryCode={CountryCode}"
};



                if (!string.IsNullOrEmpty(request.ReturnDate))
                {
                    queryParams.Add($"returnDate={Uri.EscapeDataString(request.ReturnDate)}");
                }

                if (request.CarriersIds != null && request.CarriersIds.Count > 0)
                {
                    queryParams.Add($"carriersIds={string.Join(",", request.CarriersIds)}");
                }

                var requestUrl = $"api/v2/flights/searchFlights?{string.Join("&", queryParams)}";

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Sky Scrapper API response for flight search: {Content}", content);

                using var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Check for API status
                if (!root.TryGetProperty("status", out var statusElement) || !statusElement.GetBoolean())
                {
                    _logger.LogWarning("Invalid response status for flight search.");
                    return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: false, data: null, error: "Invalid response from API."));
                }

                // Extract the session ID
                if (!root.TryGetProperty("sessionId", out var sessionIdElement) || sessionIdElement.ValueKind != JsonValueKind.String)
                {
                    _logger.LogWarning("Session ID not found in the API response.");
                    return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: false, data: null, error: "Session ID not found in the response."));
                }
                var sessionId = sessionIdElement.GetString();

                if (!root.TryGetProperty("data", out var dataElement) || !dataElement.TryGetProperty("itineraries", out var itinerariesElement) || itinerariesElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("No flights found for the given criteria.");
                    return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: true, data: new List<SearchFlightResult>()));
                }

                var flightResults = new List<SearchFlightResult>();
                foreach (var itinerary in itinerariesElement.EnumerateArray())
                {
                    var result = new SearchFlightResult
                    {
                        Id = itinerary.GetProperty("id").GetString(),
                        SessionId = sessionId,
                        AirlineName = itinerary.GetProperty("legs")[0].GetProperty("carriers").GetProperty("marketing")[0].GetProperty("name").GetString(),
                        Price = itinerary.GetProperty("price").GetProperty("formatted").GetString(),
                        IsFreeCancellation = itinerary.GetProperty("farePolicy").GetProperty("isCancellationAllowed").GetBoolean()
                    };



                    var outboundLeg = itinerary.GetProperty("legs")[0];
                    var outboundDeparture = DateTime.Parse(outboundLeg.GetProperty("departure").GetString());
                    var outboundArrival = DateTime.Parse(outboundLeg.GetProperty("arrival").GetString());
                    var outboundDuration = outboundLeg.GetProperty("durationInMinutes").GetInt32();
                    result.OutboundLeg = new FlightLeg
                    {
                        DepartureDate = outboundDeparture.ToString("yyyy-MM-dd"), // Extract date
                        DepartureTime = outboundDeparture.ToString("hh:mm tt"),
                        ArrivalDate = outboundArrival.ToString("yyyy-MM-dd"),     // Extract date
                        ArrivalTime = outboundArrival.ToString("hh:mm tt"),
                        StopCount = outboundLeg.GetProperty("stopCount").GetInt32(),
                        Duration = FormatDuration(outboundDuration)
                    };

                    // Return leg (legs[1])
                    var returnLeg = itinerary.GetProperty("legs")[1];
                    var returnDeparture = DateTime.Parse(returnLeg.GetProperty("departure").GetString());
                    var returnArrival = DateTime.Parse(returnLeg.GetProperty("arrival").GetString());
                    var returnDuration = returnLeg.GetProperty("durationInMinutes").GetInt32();
                    result.ReturnLeg = new FlightLeg
                    {
                        DepartureDate = returnDeparture.ToString("yyyy-MM-dd"), // Extract date
                        DepartureTime = returnDeparture.ToString("hh:mm tt"),
                        ArrivalDate = returnArrival.ToString("yyyy-MM-dd"),     // Extract date
                        ArrivalTime = returnArrival.ToString("hh:mm tt"),
                        StopCount = returnLeg.GetProperty("stopCount").GetInt32(),
                        Duration = FormatDuration(returnDuration)
                    };

                    flightResults.Add(result);
                }

                return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: true, data: flightResults));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch flights due to network or API error.");
                return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: false, data: null, error: "Failed to connect to Sky Scrapper API."));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Sky Scrapper API response for flight search.");
                return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: false, data: null, error: "Invalid API response format."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while searching flights.");
                return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(success: false, data: null, error: "An unexpected error occurred."));
            }
        }



    }
}