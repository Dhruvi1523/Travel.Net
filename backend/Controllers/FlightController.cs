using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightController : Controller
    {

        private readonly IFlightService _flightService;
        private readonly ILogger<FlightController> _logger;
        public FlightController(
            IFlightService flightService,
            ILogger<FlightController> logger)
        {
            _flightService = flightService;
            _logger = logger;
        }

        [HttpGet("search-airport")]
        public async Task<IActionResult> SearchAirport([FromQuery] string query){
            var result = await _flightService.SearchAirportAsync(query);
            return Ok(result);
        }
    }
}