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

        [HttpGet]
        public IActionResult CreateEvent()
        {
            var model = new EventViewModel
            {
                EventDate = DateTime.Today.AddDays(1)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(
            EventViewModel model)
        {
            // Validate the form
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Send event information to RaceDay API
                using var response =
                    await _apiService.CreateEventAsync(model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event created successfully!";

                    return RedirectToAction("Events");
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid event details. Please check your information.");

                    return View(model);
                }

                _logger.LogWarning(
                    "Event creation failed with status {StatusCode}",
                    response.StatusCode);

                ModelState.AddModelError(
                    "",
                    "Unable to create event. Please try again.");

                return View(model);
            }
            catch (InvalidOperationException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Failed to connect to RaceDay API.");

                ModelState.AddModelError(
                    "",
                    "Unable to connect to the server.");

                return View(model);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Event creation request timed out.");

                ModelState.AddModelError(
                    "",
                    "The request timed out. Please try again.");

                return View(model);
            }
        }


    }
}
