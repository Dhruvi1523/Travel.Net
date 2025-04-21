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
    
    public interface IFlightService
    {
        
        Task<string> SearchAirportAsync(string query);
        Task<string> SearchFlightsAsync(SearchFlightsRequest request);
        Task<string> GetFlightDetailsAsync(GetFlightDetailsRequest request);
    }

 
    public class FlightService : IFlightService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FlightService> _logger;
        private readonly string CountryCode = "IN";
        private readonly string Market = "en-GB";
        private readonly string Currency = "INR";
        private readonly string Locale = "en-GB";

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
                            : new List<string>(),
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
        private Leg ParseFlightLeg(JsonElement leg)
        {
            var flightLeg = new Leg();

            try
            {
                // Assign LegId
                flightLeg.LegId = leg.GetProperty("id").GetString();

                // Assign Origin Airport details
                if (leg.TryGetProperty("origin", out var origin))
                {
                    flightLeg.Origin = new Airport
                    {
                        AirportEntityId = origin.GetProperty("id").GetString(),
                        AirportName = origin.GetProperty("name").GetString(),
                        DisplayCode = origin.GetProperty("displayCode").GetString(),
                        CityName = origin.TryGetProperty("city", out var city) ? city.GetString() : "Unknown"
                    };
                }

                // Assign Destination Airport details
                if (leg.TryGetProperty("destination", out var destination))
                {
                    flightLeg.Destination = new Airport
                    {
                        AirportEntityId = destination.GetProperty("id").GetString(),
                        AirportName = destination.GetProperty("name").GetString(),
                        DisplayCode = destination.GetProperty("displayCode").GetString(),
                        CityName = destination.TryGetProperty("city", out var city) ? city.GetString() : "Unknown"
                    };
                }

                // Assign Departure and Arrival time (Safe Parsing)
                if (leg.TryGetProperty("departure", out var departure) && DateTime.TryParse(departure.GetString(), out var departureTime))
                {
                    flightLeg.Departure = departureTime;
                }
                if (leg.TryGetProperty("arrival", out var arrival) && DateTime.TryParse(arrival.GetString(), out var arrivalTime))
                {
                    flightLeg.Arrival = arrivalTime;
                }

                // Assign StopCount
                flightLeg.StopCount = leg.TryGetProperty("stopCount", out var stopCount) ? stopCount.GetInt32() : 0;

                // Assign Duration
                flightLeg.Duration = leg.TryGetProperty("durationInMinutes", out var duration) ? FormatDuration(duration.GetInt32()) : "Unknown";

                // Assign DayChange
                flightLeg.DayChange = leg.TryGetProperty("timeDeltaInDays", out var dayChange) ? dayChange.GetInt32() : 0;

                // Assign Flight Number from segments[0]
                if (leg.TryGetProperty("segments", out var segments) && segments.ValueKind == JsonValueKind.Array && segments.GetArrayLength() > 0)
                {
                    var firstSegment = segments.EnumerateArray().First();
                    flightLeg.FlightNumber = firstSegment.TryGetProperty("flightNumber", out var flightNumber) ? flightNumber.GetString() : "N/A";
                }

                // Assign Marketing Carrier from carriers.marketing array
                if (leg.TryGetProperty("carriers", out var carriers) &&
                    carriers.TryGetProperty("marketing", out var marketing) &&
                    marketing.ValueKind == JsonValueKind.Array && marketing.GetArrayLength() > 0)  // Ensure it's an array and has at least one element
                {
                    var firstMarketingCarrier = marketing.EnumerateArray().First();
                    if (firstMarketingCarrier.ValueKind == JsonValueKind.Object)  // Ensure the first element is an object
                    {
                        flightLeg.MarketingCarrier = new Carrier
                        {
                            CarrierId = firstMarketingCarrier.GetProperty("id").GetRawText(),  // Handle negative numbers properly
                            CarrierName = firstMarketingCarrier.GetProperty("name").GetString(),
                            AlternatedId = firstMarketingCarrier.TryGetProperty("alternateId", out var altId) ? altId.GetString() : "N/A",
                            CarrierLogo = firstMarketingCarrier.TryGetProperty("logoUrl", out var logoUrl) ? logoUrl.GetString() : "N/A"
                        };
                    }
                }

                if (leg.TryGetProperty("segments", out var segments2) &&
                    segments2.ValueKind == JsonValueKind.Array &&
                    segments2.GetArrayLength() > 0)
                {
                    var firstSegment = segments2.EnumerateArray().First();
                    if (firstSegment.TryGetProperty("operatingCarrier", out var operatingCarrier) &&
                        operatingCarrier.ValueKind == JsonValueKind.Object) // Ensure it's an object
                    {
                        flightLeg.OperatingCarrier = new Carrier
                        {
                            CarrierId = operatingCarrier.GetProperty("id").GetRawText(),
                            CarrierName = operatingCarrier.GetProperty("name").GetString(),
                            AlternatedId = operatingCarrier.TryGetProperty("alternateId", out var altId) ? altId.GetString() : "N/A"
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing leg: {ex.Message}");
            }

            return flightLeg;
        }




        public async Task<string> GetFlightDetailsAsync(GetFlightDetailsRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("GetFlightDetailsAsync called with null request.");
                return JsonSerializer.Serialize(new ApiResponse<string>(false, null, "Request cannot be null."));
            }

            if (string.IsNullOrEmpty(request.ItineraryId) || string.IsNullOrEmpty(request.SessionId) || request.Legs == null || !request.Legs.Any())
            {
                _logger.LogWarning("GetFlightDetailsAsync called with invalid request: Missing required fields.");
                return JsonSerializer.Serialize(new ApiResponse<string>(false, null, "Missing required fields: itineraryId, sessionId, and legs are required."));
            }

            if (request.Legs.Any(leg => string.IsNullOrEmpty(leg.Origin) || string.IsNullOrEmpty(leg.Destination) || string.IsNullOrEmpty(leg.Date)))
            {
                _logger.LogWarning("GetFlightDetailsAsync called with invalid legs: Each leg must have origin, destination, and date.");
                return JsonSerializer.Serialize(new ApiResponse<string>(false, null, "Invalid legs: Each leg must have origin, destination, and date."));
            }

            try
            {
                _logger.LogInformation("Fetching flight details for itineraryId: {ItineraryId}, sessionId: {SessionId}",
                    request.ItineraryId, request.SessionId);

                // Add query parameters in the exact order as the required URL
                var queryParams = new List<string>
        {
            $"itineraryId={Uri.EscapeDataString(request.ItineraryId)}"
        };

                // Format `legs` manually to include escaped quotes
                var legsList = new List<string>();
                foreach (var leg in request.Legs)
                {
                    // Manually construct the JSON string with escaped quotes
                    var legJson = $"{{\\\"destination\\\": \\\"{leg.Destination}\\\", \\\"origin\\\": \\\"{leg.Origin}\\\", \\\"date\\\": \\\"{leg.Date}\\\"}}";
                    legsList.Add(legJson);
                }
                var legsJson = $"[{string.Join(",", legsList)}]";
                var legsJsonWithQuotes = $"\"{legsJson}\"";
                queryParams.Add($"legs={Uri.EscapeDataString(legsJsonWithQuotes)}");

                queryParams.Add($"sessionId={Uri.EscapeDataString(request.SessionId)}");

                queryParams.Add($"adults={request.Adults}");
                queryParams.Add($"children={request.Children}");
                queryParams.Add($"currency={Uri.EscapeDataString(Currency)}");
                queryParams.Add($"locale={Uri.EscapeDataString(Locale)}");
                queryParams.Add($"market={Uri.EscapeDataString(Market)}");
                queryParams.Add($"countryCode={Uri.EscapeDataString(CountryCode)}");
                queryParams.Add($"cabinClass={Uri.EscapeDataString(request.CabinClass?.ToLower() ?? "economy")}");

                var requestUrl = $"api/v1/flights/getFlightDetails?{string.Join("&", queryParams)}";
                _logger.LogDebug("Request URL: {RequestUrl}", requestUrl);

                // Log the headers being sent
                _logger.LogDebug("Request Headers: {Headers}", string.Join(", ", _httpClient.DefaultRequestHeaders.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("API response: {Content}", content);

                // Step 1: Parse the JSON using JsonDocument to check the status
                using var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Check the status
                if (!root.TryGetProperty("status", out var statusElement) || !statusElement.GetBoolean())
                {
                    _logger.LogWarning("API response status is false.");
                    return JsonSerializer.Serialize(new ApiResponse<string>(false, null, "API response status is false."));
                }

                // Step 3: Extract the "data" property
                if (!root.TryGetProperty("data", out var dataElement) || !dataElement.TryGetProperty("itinerary", out var itinerary))
                {
                    _logger.LogWarning("Missing itinerary in API response.");
                    return JsonSerializer.Serialize(new ApiResponse<string>(false, null, "Missing itinerary in API response."));
                }

                // Extract legs
                var legsArray = itinerary.TryGetProperty("legs", out var legsProperty) ? legsProperty.EnumerateArray().ToList() : new List<JsonElement>();

                // Step 4: Determine the overall origin and destination
                if (legsArray.Count == 0)
                {
                    _logger.LogWarning("No legs found in the itinerary.");
                    return JsonSerializer.Serialize(new ApiResponse<string>(false, null, "No legs found in the itinerary."));
                }

                JsonElement firstLeg = legsArray.First();
                JsonElement lastLeg = legsArray.Last();
                var mappedLegs = new List<Leg>(); // List to store all mapped legs

                foreach (var currentLeg in legsArray)
                {
                    if (!currentLeg.TryGetProperty("segments", out var segmentsElement) || !segmentsElement.EnumerateArray().Any())
                    {
                        continue; // Skip legs with no segments
                    }

                    var segment = segmentsElement.EnumerateArray().First();

                    var mappedLeg = new Leg
                    {
                        LegId = currentLeg.GetProperty("id").GetString(),
                        FlightNumber = segment.TryGetProperty("flightNumber", out var flightNumberElement) ? flightNumberElement.GetString() : "N/A",
                        Origin = new Airport
                        {
                            AirportEntityId = segment.GetProperty("origin").GetProperty("id").GetString(),
                            AirportName = segment.GetProperty("origin").GetProperty("name").GetString(),
                            DisplayCode = segment.GetProperty("origin").GetProperty("displayCode").GetString(),
                            CityName = segment.GetProperty("origin").GetProperty("city").GetString()
                        },
                        Destination = new Airport
                        {
                            AirportEntityId = segment.GetProperty("destination").GetProperty("id").GetString(),
                            AirportName = segment.GetProperty("destination").GetProperty("name").GetString(),
                            DisplayCode = segment.GetProperty("destination").GetProperty("displayCode").GetString(),
                            CityName = segment.GetProperty("destination").GetProperty("city").GetString()
                        },
                        MarketingCarrier = new Carrier
                        {
                            CarrierId = segment.GetProperty("marketingCarrier").GetProperty("id").GetString(),
                            CarrierName = segment.GetProperty("marketingCarrier").GetProperty("name").GetString(),
                            DisplayCode = segment.GetProperty("marketingCarrier").GetProperty("displayCode").GetString()
                        },
                        OperatingCarrier = new Carrier
                        {
                            CarrierId = segment.GetProperty("operatingCarrier").GetProperty("id").GetString(),
                            CarrierName = segment.GetProperty("operatingCarrier").GetProperty("name").GetString(),
                            DisplayCode = segment.GetProperty("operatingCarrier").GetProperty("displayCode").GetString()
                        },
                        Departure = DateTime.Parse(segment.GetProperty("departure").GetString()),
                        Arrival = DateTime.Parse(segment.GetProperty("arrival").GetString()),
                        StopCount = currentLeg.GetProperty("stopCount").GetInt32(),
                        Duration = FormatDuration(currentLeg.GetProperty("duration").GetInt32()),
                        DayChange = currentLeg.GetProperty("dayChange").GetInt32()
                    };

                    mappedLegs.Add(mappedLeg);
                }

                // Step 5: Map the data to GetFlightDetailsResponse
                var result = new GetFlightDetailsResponse
                {
                    ItineraryId = itinerary.GetProperty("id").GetString(),
                    Origin = new Airport
                    {
                        AirportEntityId = firstLeg.GetProperty("origin").GetProperty("id").GetString(),
                        AirportName = firstLeg.GetProperty("origin").GetProperty("name").GetString(),
                        DisplayCode = firstLeg.GetProperty("origin").GetProperty("displayCode").GetString(),
                        CityName = firstLeg.GetProperty("origin").GetProperty("city").GetString()
                    },
                    Destination = new Airport
                    {
                        AirportEntityId = lastLeg.GetProperty("destination").GetProperty("id").GetString(),
                        AirportName = lastLeg.GetProperty("destination").GetProperty("name").GetString(),
                        DisplayCode = lastLeg.GetProperty("destination").GetProperty("displayCode").GetString(),
                        CityName = lastLeg.GetProperty("destination").GetProperty("city").GetString()
                    },
                    legs = mappedLegs, // Use the list of mapped legs
                    DestinationImage = itinerary.TryGetProperty("destinationImage", out var destImgElement) ? destImgElement.GetString() : null,
                    BookingSessionId = dataElement.TryGetProperty("bookingSessionId", out var bookingSessionId) ? bookingSessionId.GetString() : null,
                    bookingAgents = itinerary.TryGetProperty("pricingOptions", out var pricingOptions) ? pricingOptions.EnumerateArray().Select(option =>
                    {
                        if (!option.TryGetProperty("agents", out var agentsArray) || !agentsArray.EnumerateArray().Any())
                        {
                            return null;
                        }
                        var agent = agentsArray.EnumerateArray().First();
                        return new BookingAgent
                        {
                            Id = option.TryGetProperty("id", out var optionId) ? optionId.GetString() : null,
                            AgentId = agent.TryGetProperty("id", out var agentId) ? agentId.GetString() : null,
                            Name = agent.TryGetProperty("name", out var agentName) ? agentName.GetString() : "Unknown",
                            BookingProposition = agent.TryGetProperty("bookingProposition", out var bookingProp) ? bookingProp.GetString() : "N/A",
                            BookingUrl = agent.TryGetProperty("url", out var bookingUrl) ? bookingUrl.GetString() : "#",
                            Price = agent.TryGetProperty("price", out var price) ? price.GetDecimal().ToString("F2") : "0.00",
                            RatingValue = agent.TryGetProperty("rating", out var rating) ? rating.TryGetProperty("value", out var ratingVal) ? ratingVal.GetDouble().ToString("F1") : "0.0" : "0.0",
                            RatingCount = agent.TryGetProperty("rating", out rating) ? rating.TryGetProperty("count", out var ratingCount) ? ratingCount.GetInt32().ToString() : "0" : "0"
                        };
                    }).Where(agent => agent != null).OrderBy(agent => decimal.Parse(agent.Price)).ToList() : new List<BookingAgent>()
                };

                // Step 6: Return the mapped result in ApiResponse<GetFlightDetailsResponse>
                return JsonSerializer.Serialize(new ApiResponse<GetFlightDetailsResponse>(true, result));


            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network/API error while fetching flight details. Status Code: {StatusCode}, Response: {Response}",
                    ex.StatusCode, ex.Message);
                return JsonSerializer.Serialize(new ApiResponse<string>(false, null, $"Failed to connect to API: {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching flight details.");
                return JsonSerializer.Serialize(new ApiResponse<string>(false, null, "An unexpected error occurred."));
            }
        }


    }
}