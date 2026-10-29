using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;
using System.Security.Claims;
using System.Text.Json;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Organiser")]
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiService _apiService;
        private readonly ILogger<OrganiserController> _logger;

        public OrganiserController(
            RaceDayApiService apiService,
            ILogger<OrganiserController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Organiser Dashboard
        public IActionResult Index()
        {
            return View();
        }

        // Display events belonging to this Organiser
        [HttpGet]
        public async Task<IActionResult> Events()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId))
            {
                return Challenge();
            }

            try
            {
                var allEvents =
                    await _apiService.GetEventListAsync();

                var myEvents = allEvents
                    .Where(e => e.OrganiserId == organiserId)
                    .OrderBy(e => e.EventDate)
                    .ToList();

                return View(myEvents);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to retrieve organiser events.");

                TempData["ErrorMessage"] =
                    "Unable to load events. Please try again.";

                return View(new List<EventViewModel>());
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid events response from API.");

                TempData["ErrorMessage"] =
                    "Unable to process event information.";

                return View(new List<EventViewModel>());
            }
        }
    }
}
