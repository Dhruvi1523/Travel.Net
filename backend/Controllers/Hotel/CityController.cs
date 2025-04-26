using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace backend.Hotel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CityController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CityController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query is required.");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://booking-com.p.rapidapi.com/v1/hotels/locations?locale=en-gb&name={Uri.EscapeDataString(query)}");

                request.Headers.Add("x-rapidapi-host", "booking-com.p.rapidapi.com");
                request.Headers.Add("x-rapidapi-key", "11ada81770msh49c3dd7cea3fe53p178a8cjsn1e0020aab805");

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"API error: {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var cities = doc.RootElement
                    .EnumerateArray()
                    .Where(e =>
                        e.TryGetProperty("name", out _) &&
                        (
                            (e.TryGetProperty("country", out var country) && country.GetString()?.Contains("India") == true) ||
                            (e.TryGetProperty("label", out var label) && label.GetString()?.Contains("India") == true)
                        )
                    )
                    .Select(e => new { name = e.GetProperty("name").GetString() })
                    .Distinct()
                    .ToList();

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
