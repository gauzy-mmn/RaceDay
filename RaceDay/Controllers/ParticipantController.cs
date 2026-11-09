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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrolEvent(
            int eventId,
            int categoryId)
        {
            // Validate the selected event and category
            if (eventId <= 0 || categoryId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a valid race category.";

                return RedirectToAction(
                    "EventDetails",
                    new { id = eventId });
            }

            try
            {
                // Send the enrolment to RaceDay API
                using var response =
                    await _apiService.EnrolEventAsync(
                        eventId, categoryId);

                // Successful enrolment
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Created)
                {
                    TempData["SuccessMessage"] =
                        "You have successfully enrolled in this event!";

                    return RedirectToAction(
                        "EventDetails",
                        new { id = eventId });
                }

                // Participant is already enrolled
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    TempData["ErrorMessage"] =
                        "You are already enrolled in this event.";

                    return RedirectToAction(
                        "EventDetails",
                        new { id = eventId });
                }

                // Invalid category selection
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
                {
                    TempData["ErrorMessage"] =
                        "Invalid category selection. " +
                        "Please choose a category from this event.";

                    return RedirectToAction(
                        "EventDetails",
                        new { id = eventId });
                }

                // Authentication failure
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                // User does not have Participant permissions
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                // Event does not exist
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                // Other unexpected API errors
                _logger.LogWarning(
                    "Enrolment failed with status {StatusCode}",
                    response.StatusCode);

                TempData["ErrorMessage"] =
                    "Unable to complete enrolment. " +
                    "Please try again.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex, "API authentication session unavailable.");

                return Challenge();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Failed to enrol in event {EventId}",
                    eventId);

                TempData["ErrorMessage"] =
                    "Unable to connect to RaceDay API.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Enrolment request timed out.");

                TempData["ErrorMessage"] =
                    "The request timed out. " +
                    "Check your enrolments before trying again.";
            }

            return RedirectToAction(
                "EventDetails",
                new { id = eventId });
        }

        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            try
            {
                // Retrieve the logged-in Participant's enrolments
                var enrolments =
                    await _apiService.GetMyEnrolmentsAsync();

                // Display the newest enrolments first
                var sortedEnrolments = enrolments
                    .OrderByDescending(e => e.EnrolmentDate)
                    .ToList();

                return View(sortedEnrolments);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to retrieve Participant enrolments.");

                if (ex.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                if (ex.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                ViewBag.ErrorMessage =
                    "Unable to load your enrolments. Please try again.";
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid enrolment data from RaceDay API.");

                ViewBag.ErrorMessage =
                    "Unable to process your enrolment information.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Participant enrolment request timed out.");

                ViewBag.ErrorMessage =
                    "The request timed out. Please try again.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex, "Participant API session is unavailable.");

                return Challenge();
            }

            // Display an error rather than pretending
            // the Participant has no enrolments
            return View(new List<EnrolmentViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            try
            {
                // Retrieve the logged-in Participant's results
                var results = await _apiService.GetMyResultsAsync();

                // Sort results by capture date, newest first
                var sortedResults = results
                    .OrderByDescending(r => r.CapturedAt)
                    .ToList();

                return View(sortedResults);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to retrieve Participant race results.");

                if (ex.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                if (ex.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                ViewBag.ErrorMessage =
                    "Unable to load your race results. Please try again.";
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid race results response from API.");

                ViewBag.ErrorMessage =
                    "Unable to process your race results.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Participant results request timed out.");

                ViewBag.ErrorMessage =
                    "The request timed out. Please try again.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Participant API session unavailable.");

                return Challenge();
            }

            // Return an empty model alongside the error message
            return View(new List<RaceResultViewModel>());
        }


    }


}
