using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using backend.Package.Models;
using backend.Package.Services; // ✅ Import the service namespace
using System.IO;

namespace  backend.Package.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly BookingService _bookingService; // ✅ BookingService object

        // ✅ KEEP ONLY THIS CONSTRUCTOR
        public PaymentController(IWebHostEnvironment env)
        {
            _env = env;
            _bookingService = new BookingService(); // ✅ Initialize BookingService manually
        }

        [HttpGet]
        public IActionResult Checkout(string id)
        {
            var filePath = Path.Combine(_env.WebRootPath, "data", "packages.json");
            var jsonData = System.IO.File.ReadAllText(filePath);
            var packages = JsonConvert.DeserializeObject<List<TourPackage>>(jsonData) ?? new List<TourPackage>();
            var selected = packages.FirstOrDefault(p => p.Id.ToLower() == id.ToLower());

            if (selected == null) return NotFound();
            return View("~/Views/Package/Payment/Checkout.cshtml",selected);
        }

        [HttpPost]
        public IActionResult Confirm(string id, string Name, string Phone, string Email)
        {
            // ✅ Save booking into MongoDB
            var booking = new Booking
            {
                PackageId = id,
                Name = Name,
                Phone = Phone,
                Email = Email
            };
            _bookingService.CreateBooking(booking);

            // ✅ Keep your TempData message
            TempData["Message"] = $"Thank you {Name}, your booking for {id} has been received!";
            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            ViewBag.Message = TempData["Message"];
            return View("~/Views/Package/Payment/Success.cshtml");
        }
    }
}
