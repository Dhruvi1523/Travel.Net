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
        // Task<string> GetFlightsDetailsAsync(GetFlightDetailsRequest request);
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
        return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(false, null, "Request cannot be null."));
    }

    try
    {
        _logger.LogInformation("Searching flights from {OriginSkyId} to {DestinationSkyId} on {Date}", 
            request.OriginSkyId, request.DestinationSkyId, request.Date);

        var queryParams = new List<string>
        {
            $"originSkyId={Uri.EscapeDataString(request.OriginSkyId)}",
            $"destinationSkyId={Uri.EscapeDataString(request.DestinationSkyId)}",
            $"originEntityId={Uri.EscapeDataString(request.OriginEntityId)}",
            $"destinationEntityId={Uri.EscapeDataString(request.DestinationEntityId)}",
            $"date={Uri.EscapeDataString(request.Date)}",
            $"cabinClass={Uri.EscapeDataString(request.CabinClass.ToLower())}",
            $"adults={request.Adults}",
            $"children={request.Children}",
            $"infants={request.Infants}",
            $"sortBy={Uri.EscapeDataString(request.SortBy.ToLower())}",
            $"limit={request.Limit}",
            $"currency={Currency}",
            $"market={Market}",
            $"countryCode={CountryCode}"
        };

        if (!string.IsNullOrEmpty(request.ReturnDate))
        {
            queryParams.Add($"returnDate={Uri.EscapeDataString(request.ReturnDate)}");
        }
        if (request.CarriersIds?.Count > 0)
        {
            queryParams.Add($"carriersIds={string.Join(",", request.CarriersIds)}");
        }

        var requestUrl = $"api/v2/flights/searchFlights?{string.Join("&", queryParams)}";
        _logger.LogDebug("Request URL: {RequestUrl}", requestUrl);

        var response = await _httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("API response: {Content}", content);

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("status", out var statusElement) || !statusElement.GetBoolean())
        {
            _logger.LogWarning("Invalid API response: Status is not true.");
            return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(false, null, "Invalid API response."));
        }

        if (!root.TryGetProperty("data", out var dataElement) ||
            !dataElement.TryGetProperty("context", out var contextElement) ||
            !contextElement.TryGetProperty("sessionId", out var sessionIdElement))
        {
            _logger.LogWarning("Invalid API response: Missing sessionId.");
            return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(false, null, "Invalid API response: Missing sessionId."));
        }

        var sessionId = sessionIdElement.GetString();
        if (!dataElement.TryGetProperty("itineraries", out var itinerariesElement))
        {
            _logger.LogWarning("Invalid API response: Missing itineraries.");
            return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(false, null, "Invalid API response: Missing itineraries."));
        }

        var itineraries = itinerariesElement.EnumerateArray();
        var flightResults = new List<SearchFlightResult>();

        foreach (var itinerary in itineraries)
        {
            if (!itinerary.TryGetProperty("legs", out var legsElement))
            {
                _logger.LogWarning("Invalid itinerary: Missing legs.");
                continue;
            }

            var legs = legsElement.EnumerateArray().ToList();
            if (legs.Count == 0)
            {
                _logger.LogWarning("Invalid itinerary: No legs found.");
                continue;
            }

            var outboundLeg = legs[0];
            var returnLeg = legs.Count > 1 ? (JsonElement?)legs[1] : null;

            var result = new SearchFlightResult
            {
                Id = itinerary.GetProperty("id").GetString(),
                SessionId = sessionId,
                Price = itinerary.GetProperty("price").GetProperty("formatted").GetString(),
                PricingOptionId = itinerary.GetProperty("price").GetProperty("pricingOptionId").GetString(),
                OutboundLeg = ParseFlightLeg(outboundLeg),
                ReturnLeg = returnLeg.HasValue ? ParseFlightLeg(returnLeg.Value) : null,
                Tags = itinerary.TryGetProperty("tags", out var tagsElement)
                    ? tagsElement.EnumerateArray().Select(tag => tag.GetString()).ToList()
                    : new List<string>() ,
             Score = itinerary.TryGetProperty("score", out var scoreElement)
        ? scoreElement.GetDouble()
        : 0.0
            };

            flightResults.Add(result);
        }

        return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(true, flightResults));
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Network/API error.");
        return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(false, null, "Failed to connect to API."));
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Failed to parse API response.");
        return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(false, null, "Invalid API response format."));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error.");
        return JsonSerializer.Serialize(new ApiResponse<List<SearchFlightResult>>(false, null, "An unexpected error occurred."));
    }
}
private FlightLeg ParseFlightLeg(JsonElement leg)
{
    var flightLeg = new FlightLeg();

    // Safely access properties with TryGetProperty
    if (leg.TryGetProperty("id", out var legId))
    {
        flightLeg.LegId = legId.GetString();
    }

    // Access segments[0] for flightNumber
    if (leg.TryGetProperty("segments", out var segments) && segments.GetArrayLength() > 0)
    {
        var firstSegment = segments[0];
        if (firstSegment.TryGetProperty("flightNumber", out var flightNumber))
        {
            flightLeg.FlightNumber = flightNumber.GetString();
        }
    }

    // Access carriers.marketing[0] for airline details
    if (leg.TryGetProperty("carriers", out var carriers) &&
        carriers.TryGetProperty("marketing", out var marketing) &&
        marketing.GetArrayLength() > 0)
    {
        var marketingCarrier = marketing[0];
        if (marketingCarrier.TryGetProperty("name", out var airlineName))
        {
            flightLeg.AirlineName = airlineName.GetString();
        }
        if (marketingCarrier.TryGetProperty("logoUrl", out var airlineLogo))
        {
            flightLeg.AirlineLogo = airlineLogo.GetString();
        }
    }

    // Access origin details
    if (leg.TryGetProperty("origin", out var origin))
    {
        if (origin.TryGetProperty("displayCode", out var originCode))
        {
            flightLeg.OriginAirportCode = originCode.GetString();
        }
        if (origin.TryGetProperty("city", out var originCity))
        {
            flightLeg.OriginCityName = originCity.GetString();
        }
    }

    // Access destination details
    if (leg.TryGetProperty("destination", out var destination))
    {
        if (destination.TryGetProperty("displayCode", out var destCode))
        {
            flightLeg.DestinationAirportCode = destCode.GetString();
        }
        if (destination.TryGetProperty("city", out var destCity))
        {
            flightLeg.DestinationCityName = destCity.GetString();
        }
    }

    // Access departure and arrival times
    if (leg.TryGetProperty("departure", out var departure))
    {
        flightLeg.Departure = DateTime.Parse(departure.GetString());
    }
    if (leg.TryGetProperty("arrival", out var arrival))
    {
        flightLeg.Arrival = DateTime.Parse(arrival.GetString());
    }

    // Access stopCount and duration
    if (leg.TryGetProperty("stopCount", out var stopCount))
    {
        flightLeg.StopCount = stopCount.GetInt32();
    }
    if (leg.TryGetProperty("durationInMinutes", out var duration))
    {
        flightLeg.Duration = FormatDuration(duration.GetInt32());
    }

    return flightLeg;
}


        // public Task<string> GetFlightsDetailsAsync(GetFlightDetailsRequest request){
            

        // }


    }
}