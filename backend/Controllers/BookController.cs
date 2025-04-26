using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

// [Authorize]
[Route("api/[controller]")]
[ApiController]
public class BookController : Controller
{
    private readonly ILogger<BookController> _logger;
    private readonly IBookingService _bookingService;

    public BookController(IBookingService bookingService, ILogger<BookController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult FillBookingForm([FromForm] string agentId, [FromForm] string itineraryId,  [FromForm] string bookingId ,[FromForm] string flightDate , [FromForm] string destination , [FromForm] string origin)
    {
        var model = new FlightBookingModel
        {
            AgentId = agentId,
            ItineraryId = itineraryId,
            BookingId = bookingId,
            FlightDate = flightDate ,
            Destination = destination ,
            Origin = origin
        };
        return View("BookingForm", model);
    }

    [HttpPost("submit-booking")]
    public async Task<IActionResult> SubmitBooking([FromBody] FlightBookingModel model)
    {
        model.UserId = "67dc6058721cea70453b3647";
        model.PaymentStatus = "Pending";

        await _bookingService.SaveBookingAsync(model);

        return Ok(
            new { redirectUrl = Url.Action("Payment", "Book", new { bookingId = model.Id }) }
        );
    }

    [HttpGet("payment")]
    public async Task<IActionResult> Payment(string bookingId)
    {
        if (string.IsNullOrEmpty(bookingId))
            return BadRequest("Booking ID is missing");

        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null)
            return NotFound("Booking not found");

        return View("Payment", booking); // Pass the model to the view
    }

    [HttpGet("confirm-booking")]
    public async Task<IActionResult> ConfirmBooking(string bookingId)
    {
        if (string.IsNullOrEmpty(bookingId))
        {
            return BadRequest("Booking ID is required.");
        }

        var booking = await _bookingService.GetBookingByIdAsync(bookingId);

        if (booking == null)
        {
            return NotFound("Booking not found.");
        }

        booking.PaymentStatus = "Confirmed";

        await _bookingService.UpdateBookingAsync(bookingId, booking);

        return View("Confirmation" , booking );
    }


    
}
