using System.Text.Json;
using Microsoft.Extensions.Logging;
using backend.Hotel.Models;

namespace backend.Hotel.Services
{   
      public interface IHotelService
    {
        Task<IEnumerable<HotelResult>> SearchHotelsAsync(
            DateTime checkIn,
            DateTime checkOut,
            int rooms,
            string? priceRange,
            string? location);
    }
    public class HotelService : IHotelService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HotelService> _logger;
        private const string ApiKey = "f108f7264cmsh98b32fd0fa26d1cp1a558ejsn2eae95ea3358";
        private const string ApiHost = "booking-com.p.rapidapi.com";

        public HotelService(IHttpClientFactory httpClientFactory, ILogger<HotelService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<HotelResult>> SearchHotelsAsync(
            DateTime checkIn,
            DateTime checkOut,
            int rooms,
            string? priceRange,
            string? location)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(BuildApiUrl(checkIn, checkOut, rooms, location)),
                    Headers =
                    {
                        { "X-RapidAPI-Host", ApiHost },
                        { "X-RapidAPI-Key", ApiKey },
                    },
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return ParseApiResponse(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for hotels");
                throw;
            }
        }

        private string BuildApiUrl(DateTime checkIn, DateTime checkOut, int rooms, string? location)
        {
            var baseUrl = "https://booking-com.p.rapidapi.com/v1/hotels/search";
            var queryParams = new Dictionary<string, string>
            {
                ["checkin_date"] = checkIn.ToString("yyyy-MM-dd"),
                ["checkout_date"] = checkOut.ToString("yyyy-MM-dd"),
                ["adults_number"] = "2",
                ["room_number"] = rooms.ToString(),
                ["dest_type"] = "city",
                ["order_by"] = "popularity",
                ["filter_by_currency"] = "AED",
                ["locale"] = "en-gb",
                ["units"] = "metric",
                ["include_adjacency"] = "true",
                ["page_number"] = "0"
            };

            if (!string.IsNullOrEmpty(location))
            {
                // In a real app, you would first need to get the dest_id for the location
                // This is simplified for demonstration
                queryParams["dest_id"] = "-553173"; // Example destination ID for Dubai
            }

            return $"{baseUrl}?{string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
        }

        private List<HotelResult> ParseApiResponse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var results = new List<HotelResult>();
            
            foreach (var hotel in root.GetProperty("result").EnumerateArray())
            {
                results.Add(new HotelResult
                {
                    Id = hotel.GetProperty("hotel_id").GetInt32(),
                    Name = hotel.GetProperty("hotel_name").GetString() ?? "Unknown",
                    Location = hotel.GetProperty("city_name").GetString() ?? "Unknown",
                    PricePerNight = hotel.GetProperty("min_total_price").GetDecimal(),
                    CheckIn = DateTime.Parse(hotel.GetProperty("checkin").GetProperty("from").GetString()!),
                    CheckOut = DateTime.Parse(hotel.GetProperty("checkout").GetProperty("until").GetString()!),
                    ImageUrl = hotel.GetProperty("main_photo_url").GetString() ?? string.Empty,
                    Amenities = GetAmenities(hotel)
                });
            }

            return results;
        }

        private List<string> GetAmenities(JsonElement hotel)
        {
            var amenities = new List<string>();
            
            if (hotel.TryGetProperty("hotel_include_breakfast", out var breakfast) && breakfast.GetBoolean())
            {
                amenities.Add("Breakfast Included");
            }
            
            if (hotel.TryGetProperty("is_free_cancellable", out var freeCancel) && freeCancel.GetBoolean())
            {
                amenities.Add("Free Cancellation");
            }
            
            // Add more amenities as needed from the API response
            
            return amenities;
        }
    }
}