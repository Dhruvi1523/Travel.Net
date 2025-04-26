using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using backend.Hotel.Models;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace backend.Hotel.Controllers
{   
    [Route("api/[controller]")]
    [ApiController]
    public class HotelController : Controller
    {
        // Store all loaded hotels in memory with assigned IDs
        private static List<HotelViewModel> _allHotels = new List<HotelViewModel>();
        private static int _hotelIdCounter = 1;

        [HttpGet("index")]
        public IActionResult Index() => View("~/views/Hotel/Home/Index.cshtml");

        [HttpGet("Goa")]
        public IActionResult Goa() => View( "~/views/Hotel/Home/Goa.cshtml",GetHotelsInGoa());

        [HttpGet("Ooty")]
        public IActionResult Ooty() => View("~/views/Hotel/Home/Ooty.cshtml",GetHotelsInOoty());

        [HttpGet("Manali")]
        public IActionResult Manali() => View("~/views/Hotel/Home/Manali.cshtml",GetHotelsInManali());

        [HttpGet("Mumbai")]
        public IActionResult Mumbai() => View("~/views/Hotel/Home/Mumbai.cshtml", GetHotelsInMumbai());

        [HttpGet("Ujjain")]
        public IActionResult Ujjain() => View("~/views/Hotel/Home/Ujjain.cshtml",GetHotelsInUjjain());

        [HttpGet("Udaipur")]
        public IActionResult Udaipur() => View("~/views/Hotel/Home/Udaipur.cshtml",GetHotelsInUdaipur());

        [HttpGet("Delhi")]
        public IActionResult Delhi() => View("~/views/Hotel/Home/Delhi.cshtml",GetHotelsInDelhi());

        [HttpGet("Banglore")]
        public IActionResult Banglore() => View("~/views/Hotel/Home/Banglore.cshtml",GetHotelsInBanglore());

        [HttpGet("Jaipur")]
        public IActionResult Jaipur() => View("~/views/Hotel/Home/Jaipur.cshtml",GetHotelsInJaipur());

        [HttpGet("Shimla")]
        public IActionResult Shimla() => View("~/views/Hotel/Home/Shimla.cshtml",GetHotelsInShimla());

        [HttpGet("details")]
        public IActionResult Details(int id)
        {
            var hotel = _allHotels.FirstOrDefault(h => h.Id == id);

            if (hotel == null)
                return NotFound();

            return View("~/Views/Hotel/Home/Details.cshtml",hotel); 
        }

        

        [HttpGet("search-city")]
        public IActionResult SearchCity(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                TempData["ErrorMessage"] = "Location is required.";
                return RedirectToAction("Index");
            }

            try
            {
                var hotels = GetHotelsByCityName(location);
                if (!hotels.Any())
                {
                    TempData["ErrorMessage"] = $"No hotels found for '{location}'.";
                }

                return View("~/Views/Hotel/Home/SearchResult.cshtml", hotels);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"API error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        
        private List<HotelViewModel> GetHotelsByCityName(string cityName)
        {
            var hotels = new List<HotelViewModel>();
            var client = new HttpClient();

            // Step 1: Get coordinates from city name
            var locRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://booking-com.p.rapidapi.com/v1/hotels/locations?name={Uri.EscapeDataString(cityName)}&locale=en-gb")
            };
            locRequest.Headers.Add("x-rapidapi-host", "booking-com.p.rapidapi.com");
            locRequest.Headers.Add("x-rapidapi-key", "3c088213e4msh02fd257f45141dap19594ajsn6564251585a0");

            var locResponse = client.SendAsync(locRequest).Result;
            var locJson = locResponse.Content.ReadAsStringAsync().Result;
            var locations = JArray.Parse(locJson);

            if (!locations.Any())
                return hotels;

            var lat = locations[0]["latitude"]?.ToString();
            var lon = locations[0]["longitude"]?.ToString();

            if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
                return hotels;

            // Step 2: Fetch hotels using coordinates
            var hotelRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://booking-com.p.rapidapi.com/v1/hotels/search-by-coordinates?children_ages=5%2C0&page_number=0&categories_filter_ids=class%3A%3A2%2Cclass%3A%3A4%2Cfree_cancellation%3A%3A1&units=metric&adults_number=2&locale=en-gb&longitude={lon}&latitude={lat}&children_number=2&room_number=1&checkin_date=2025-10-13&include_adjacency=true&filter_by_currency=AED&order_by=popularity&checkout_date=2025-10-14")
            };
            hotelRequest.Headers.Add("x-rapidapi-host", "booking-com.p.rapidapi.com");
            hotelRequest.Headers.Add("x-rapidapi-key", "3c088213e4msh02fd257f45141dap19594ajsn6564251585a0");

            var hotelResponse = client.SendAsync(hotelRequest).Result;
            var hotelJson = hotelResponse.Content.ReadAsStringAsync().Result;

            var hotelData = JObject.Parse(hotelJson);
            var resultArray = hotelData["result"];

            if (resultArray != null)
            {
                foreach (var h in resultArray)
                {
                    var hotel = new HotelViewModel
                    {
                        Id = _hotelIdCounter++,
                        Name = h["hotel_name"]?.ToString() ?? "N/A",
                        Address = h["address"]?.ToString() ?? "Not available",
                        ImageUrl = h["main_photo_url"]?.ToString()?.Replace("square60", "max1024x768") ?? "/images/no-image.jpg",
                        PricePerNight = h["min_total_price"]?.Value<decimal?>() ?? 0,
                        Rating = h["review_score"]?.Value<double?>() ?? 0,
                        ReviewCount = h["review_nr"]?.Value<int?>() ?? 0,
                        Discount = h["cant_book"]?.ToString() == "False" ? 421 : 0
                    };

                    hotels.Add(hotel);
                    _allHotels.Add(hotel);
                }
            }

            return hotels;
        }


                // === Per-city data fetch methods ===
                private List<HotelViewModel> GetHotelsInGoa() => GetHotelsFromCoordinates(15.2993, 74.1240);
                private List<HotelViewModel> GetHotelsInOoty() => GetHotelsFromCoordinates(11.4064, 76.6932);
                private List<HotelViewModel> GetHotelsInManali() => GetHotelsFromCoordinates(32.2396, 77.1887);
                private List<HotelViewModel> GetHotelsInMumbai() => GetHotelsFromCoordinates(19.0760, 72.8777);
                private List<HotelViewModel> GetHotelsInUjjain() => GetHotelsFromCoordinates(23.1793, 75.7849);
                private List<HotelViewModel> GetHotelsInUdaipur() => GetHotelsFromCoordinates(24.5854, 73.7125);
                private List<HotelViewModel> GetHotelsInDelhi() => GetHotelsFromCoordinates(28.6139, 77.2090);
                private List<HotelViewModel> GetHotelsInBanglore() => GetHotelsFromCoordinates(12.9716, 77.5946);
                private List<HotelViewModel> GetHotelsInJaipur() => GetHotelsFromCoordinates(26.9124, 75.7873);
                private List<HotelViewModel> GetHotelsInShimla() => GetHotelsFromCoordinates(31.1048, 77.1734);

        [HttpPost("book-hotel")]
public IActionResult Booking([FromForm] BookingViewModel model)
{
    if (ModelState.IsValid)
    {
        // You can add booking to database or session here

        // Show confirmation or redirect
        return View("~/Views/Hotel/Home/Booking.cshtml", model); // or "Booking.cshtml" if that's your page
    }

    return View("~/Views/Hotel/Home/Details.cshtml", model); // In case of errors, re-show form
}
        private List<HotelViewModel> GetHotelsFromCoordinates(double latitude, double longitude)
        {
            var hotelList = new List<HotelViewModel>();

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://booking-com.p.rapidapi.com/v1/hotels/search-by-coordinates?children_ages=5%2C0&include_adjacency=true&adults_number=2&checkout_date=2025-09-26&filter_by_currency=INR&checkin_date=2025-09-25&categories_filter_ids=class%3A%3A2%2Cclass%3A%3A4%2Cfree_cancellation%3A%3A1&units=metric&order_by=popularity&children_number=2&locale=en-gb&page_number=0&room_number=1&latitude={latitude}&longitude={longitude}"),
                    Headers =
                    {
                        { "x-rapidapi-host", "booking-com.p.rapidapi.com" },
                        { "x-rapidapi-key", "3c088213e4msh02fd257f45141dap19594ajsn6564251585a0" }
                    }
                };

                var response = client.SendAsync(request).Result;
                var json = response.Content.ReadAsStringAsync().Result;
                var data = JObject.Parse(json);
                var results = data["result"];

                if (results != null)
                {
                    foreach (var hotel in results)
                    {
                        var newHotel = new HotelViewModel
                        {
                            Id = _hotelIdCounter++, // 🔥 assign unique ID
                            Name = hotel["hotel_name"]?.ToString() ?? "N/A",
                            Address = hotel["address"]?.ToString() ?? "Not available",
                            ImageUrl = hotel["main_photo_url"]?.ToString()?.Replace("square60", "max1024x768") ?? "/images/no-image.jpg",
                            PricePerNight = hotel["min_total_price"]?.Value<decimal?>() ?? 0,
                            Rating = hotel["review_score"]?.Value<double?>() ?? 0,
                            ReviewCount = hotel["review_nr"]?.Value<int?>() ?? 0,
                            Discount = hotel["cant_book"]?.ToString() == "False" ? 421 : 0
                        };

                        hotelList.Add(newHotel);
                        _allHotels.Add(newHotel); // ✅ Store for use in Details()
                    }
                }
            }

            return hotelList;
        }
    }
}
