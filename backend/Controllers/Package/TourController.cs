using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using backend.Package.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace  backend.Package.Controllers
{
    public class TourController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public TourController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            var filePath = Path.Combine(_env.WebRootPath, "data", "packages.json");
            var jsonData = System.IO.File.ReadAllText(filePath);
            var packages = JsonConvert.DeserializeObject<List<TourPackage>>(jsonData) ?? new List<TourPackage>();
            return View("~/Views/Package/Tour/Index.cshtml",packages);
        }

        public IActionResult Details(string id)
        {
            var filePath = Path.Combine(_env.WebRootPath, "data", "packages.json");
            var jsonData = System.IO.File.ReadAllText(filePath);
            var packages = JsonConvert.DeserializeObject<List<TourPackage>>(jsonData) ?? new List<TourPackage>();

            var selected = packages.FirstOrDefault(p => p.Id.ToLower() == id.ToLower());

            if (selected == null)
                return NotFound();

            return View("~/Views/Package/Tour/Details.cshtml",selected);
        }

    }
}