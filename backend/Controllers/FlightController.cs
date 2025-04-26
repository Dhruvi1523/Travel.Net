using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;


namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightController : Controller
    {
        private readonly IFlightService _flightService;
        private readonly ILogger<FlightController> _logger;

        public FlightController(IFlightService flightService, ILogger<FlightController> logger)
        {
            _flightService = flightService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("booking")]
        public IActionResult FlightBooking(){
            return View();
        }
        [HttpGet("flight-data")]
       public IActionResult GetFlightDetailsView()
{
    var json = TempData["FlightJson"] as string;

    // if (json != null)
    // {
    //     // var model = JsonSerializer.Deserialize<ApiResponse<GetFlightDetailsResponse>>(json);
    //     // return View("FlightBooking", model.Data); // Pass data to the view
       

    // }
    // return View("Error"); // If no data in TempData


    GetFlightDetailsResponse data = new GetFlightDetailsResponse 
    {
    ItineraryId = "10957-2504281615--32672-0-13867-2504281910",
    BookingSessionId = "6b845220-9637-47a6-a694-7c993869f27e",
    Origin = new Airport {
        AirportEntityId = "10957",
        AirportName = "Indira Gandhi International",
        CityName = "New Delhi",
        DisplayCode = "DEL"
    },
    Destination = new Airport {
        AirportEntityId = "13867",
        AirportName = "Chennai",
        CityName = "Chennai",
        DisplayCode = "MAA"
    },
    DestinationImage = "https://content.skyscnr.com/6444159c8f0ec14c96012de8a2502245/eyeem-26693940-86115263.jpg",
    legs = new List<Leg> {
        new Leg {
            LegId = "10957-2504281615--32672-0-13867-2504281910",
            FlightNumber = "AI537",
            Origin = new Airport {
                AirportEntityId = "10957",
        AirportName = "Indira Gandhi International",
        CityName = "New Delhi",
        DisplayCode = "DEL"
             }, // same as above
            Destination = new Airport {
                 AirportEntityId = "13867",
        AirportName = "Chennai",
        CityName = "Chennai",
        DisplayCode = "MAA"
             }, // same as above
            MarketingCarrier = new Carrier {
                CarrierId = "-32672",
                CarrierName = "Air India",
                DisplayCode = "AI"
            },
            OperatingCarrier = new Carrier {
                CarrierId = "-32672",
                CarrierName = "Air India",
                DisplayCode = "AI"
            },
            Departure = DateTime.Parse("2025-04-28T16:15:00"),
            Arrival = DateTime.Parse("2025-04-28T19:10:00"),
            StopCount = 0,
            
            Duration = "2h 55m",
            DayChange = 0
        }
    },
    bookingAgents = new List<BookingAgent> {
        new BookingAgent {
            Id = "jGga-V5QKnmc",
            AgentId = "stbf",
            Name = "Sky-tours",
            Price = "72.00",
            RatingValue = "2.2",
            RatingCount = "1108",
            BookingUrl = ""
        },
        new BookingAgent {
            Id = "fFaMp_8-Quch",
            AgentId = "cust",
            Name = "Trip.com",
            Price = "74.40",
            RatingValue = "4.5",
            RatingCount = "7781",
            BookingUrl = ""
        },
        
    }
};

      return View("FlightBooking", data);

}


        [HttpGet("search-airport")]
        public async Task<IActionResult> SearchAirport([FromQuery] string query)
        {
            var result = await _flightService.SearchAirportAsync(query);
            return Ok(result);
        }

        [HttpPost("search-flight")]
        public async Task<IActionResult> SearchFlights([FromBody] SearchFlightsRequest request)
        {
            var result = await _flightService.SearchFlightsAsync(request);
            return Ok(result);
        }

        [HttpPost("get-flight-details")]
        public async Task<IActionResult> GetFlightDetails([FromBody] GetFlightDetailsRequest request)
        {
            var result = await _flightService.GetFlightDetailsAsync(request);
            var response = JsonSerializer.Deserialize<ApiResponse<GetFlightDetailsResponse>>(result);

            if (response != null && response.Success)
            {
                TempData["FlightJson"] = JsonSerializer.Serialize(response);
                return Json(new { redirectUrl = Url.Action("GetFlightDetailsView") });  // Return redirect URL in JSON
            }

            return Json(new { redirectUrl = Url.Action("Error") });  // Return error URL in case of failure
        }


    }
}
