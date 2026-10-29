
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;
using System.Diagnostics;

namespace RaceDay.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RaceDayApiService _apiService;

        // Constructor
        public HomeController(
            ILogger<HomeController> logger,
            RaceDayApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        // Home page
        public IActionResult Index()
        {
            return View();
        }

        // Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Retrieve events from RaceDay API
        public async Task<IActionResult> Events()
        {
            try
            {
                var events = await _apiService.GetEventsAsync();

                return Content(events, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Failed to retrieve events from API");

                return StatusCode(
                    503,
                    "Unable to load events. Please try again later.");
            }
        }

        // Error page
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
