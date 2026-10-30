using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;
using System.Text.Json;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Participant")]
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiService _apiService;
        private readonly ILogger<ParticipantController> _logger;

        public ParticipantController(
            RaceDayApiService apiService,
            ILogger<ParticipantController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Participant Dashboard

        public IActionResult Index()
        {
            return RedirectToAction("Events");
        }


        // Browse upcoming race events
        [HttpGet]
        public async Task<IActionResult> Events(
            string? location,
            string? eventType)
        {
            try
            {
                // Retrieve events from the existing API
                var allEvents =
                    await _apiService.GetEventListAsync();

                // Only display upcoming events
                var events = allEvents
                    .Where(e => e.EventDate >= DateTime.UtcNow)
                    .OrderBy(e => e.EventDate)
                    .ToList();

                // Filter by location
                if (!string.IsNullOrWhiteSpace(location))
                {
                    events = events
                        .Where(e => e.Location.Contains(
                            location,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Filter by event type
                if (!string.IsNullOrWhiteSpace(eventType))
                {
                    events = events
                        .Where(e => string.Equals(
                            e.EventType,
                            eventType,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                ViewBag.Location = location;
                ViewBag.EventType = eventType;

                return View(events);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to retrieve race events.");

                ViewBag.ErrorMessage =
                    "Unable to load events. Please try again.";

                return View(new List<EventViewModel>());
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid event information from API.");

                ViewBag.ErrorMessage =
                    "Unable to process event information.";

                return View(new List<EventViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> EventDetails(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                // Retrieve the selected event from the API
                var raceEvent =
                    await _apiService.GetEventByIdAsync(id);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Retrieve categories for this event
                var categories =
                    await _apiService.GetEventCategoriesAsync(id);

                // Combine event details and categories
                var model = new EventDetailsViewModel
                {
                    Event = raceEvent,
                    Categories = categories
                };

                // Display the Event Details page
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to retrieve event details for {EventId}", id);

                if (ex.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] =
                    "Unable to load event details. Please try again.";

                return RedirectToAction("Events");
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid event details response for {EventId}", id);

                TempData["ErrorMessage"] =
                    "Unable to process event information.";

                return RedirectToAction("Events");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Event details request timed out for {EventId}", id);

                TempData["ErrorMessage"] =
                    "The request timed out. Please try again.";

                return RedirectToAction("Events");
            }
        }

    }


}
