using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetFlightDetails(
            [FromBody] GetFlightDetailsRequest request
        )
        {
            var result = await _flightService.GetFlightDetailsAsync(request);
            return Ok(result);
        }
    }
}
