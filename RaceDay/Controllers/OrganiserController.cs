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
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                // Retrieve the event from RaceDay API
                var raceEvent =
                    await _apiService.GetEventByIdAsync(id);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Get the logged-in Organiser's ID
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int organiserId))
                {
                    return Challenge();
                }

                // Check event ownership
                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                // Display the Edit Event form
                return View(raceEvent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to retrieve event {EventId}", id);

                TempData["ErrorMessage"] =
                    "Unable to load event details.";

                return RedirectToAction("Events");
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid event response for {EventId}", id);

                TempData["ErrorMessage"] =
                    "Unable to process event details.";

                return RedirectToAction("Events");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(
            int id, EventViewModel model)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                model.EventId = id;
                return View(model);
            }

            try
            {
                // Send updated information to the API
                using var response =
                    await _apiService.UpdateEventAsync(id, model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event updated successfully!";

                    return RedirectToAction("Events");
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                ModelState.AddModelError(
                    "",
                    "Unable to update event. Check the details.");

                model.EventId = id;
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to update event {EventId}", id);

                ModelState.AddModelError(
                    "",
                    "Unable to connect to RaceDay API.");

                model.EventId = id;
                return View(model);
            }
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

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                var raceEvent =
                    await _apiService.GetEventByIdAsync(id);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Verify event ownership
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int organiserId))
                {
                    return Challenge();
                }

                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                // Display confirmation page
                return View(raceEvent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Failed to load event {EventId}", id);

                TempData["ErrorMessage"] =
                    "Unable to load event details.";

                return RedirectToAction("Events");
            }
        }

        [HttpPost]
        [ActionName("DeleteEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                // Request deletion through RaceDay API
                using var response =
                    await _apiService.DeleteEventAsync(id);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event deleted successfully!";

                    return RedirectToAction("Events");
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                _logger.LogWarning(
                    "Event deletion failed: {StatusCode}",
                    response.StatusCode);

                TempData["ErrorMessage"] =
                    "Unable to delete event. It may have related records.";

                return RedirectToAction("Events");
            }
            catch (InvalidOperationException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to delete event {EventId}", id);

                TempData["ErrorMessage"] =
                    "Unable to connect to RaceDay API.";

                return RedirectToAction("Events");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Deletion request timed out for event {EventId}", id);

                TempData["ErrorMessage"] =
                    "Request timed out. Check whether the event still exists.";

                return RedirectToAction("Events");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Categories(int eventId)
        {
            if (eventId <= 0)
            {
                return BadRequest();
            }

            try
            {
                // Retrieve the selected event
                var raceEvent =
                    await _apiService.GetEventByIdAsync(eventId);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Identify the logged-in Organiser
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int organiserId))
                {
                    return Challenge();
                }

                // Only allow the owner to manage categories
                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                // Retrieve categories from the API
                var categories =
                    await _apiService.GetEventCategoriesAsync(eventId);

                ViewBag.EventId = eventId;
                ViewBag.EventName = raceEvent.Name;

                return View(categories);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load categories for event {EventId}",
                    eventId);

                TempData["ErrorMessage"] =
                    "Unable to load event categories.";

                return RedirectToAction("Events");
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid categories response for event {EventId}",
                    eventId);

                TempData["ErrorMessage"] =
                    "Unable to process category information.";

                return RedirectToAction("Events");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateCategory(int eventId)
        {
            if (eventId <= 0)
            {
                return BadRequest();
            }

            try
            {
                var raceEvent =
                    await _apiService.GetEventByIdAsync(eventId);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int organiserId))
                {
                    return Challenge();
                }

                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                ViewBag.EventName = raceEvent.Name;

                return View(new CategoryViewModel
                {
                    EventId = eventId
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to load event {EventId}", eventId);

                TempData["ErrorMessage"] =
                    "Unable to load event information.";

                return RedirectToAction("Events");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            CategoryViewModel model)
        {
            if (eventId <= 0)
            {
                return BadRequest();
            }

            model.EventId = eventId;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using var response =
                    await _apiService.CreateCategoryAsync(
                        eventId, model);

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Created)
                {
                    TempData["SuccessMessage"] =
                        "Category created successfully!";

                    return RedirectToAction(
                        "Categories",
                        new { eventId });
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        nameof(model.Name),
                        "A category with this name already exists.");

                    return View(model);
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid category details.");

                    return View(model);
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
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                ModelState.AddModelError(
                    "",
                    "Unable to create category. Please try again.");

                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Category creation failed for event {EventId}",
                    eventId);

                ModelState.AddModelError(
                    "",
                    "Unable to connect to RaceDay API.");

                return View(model);
            }
            catch (InvalidOperationException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Category creation timed out.");

                ModelState.AddModelError(
                    "",
                    "Request timed out. Check the category list before retrying.");

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(
            int eventId, int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
            {
                return BadRequest();
            }

            try
            {
                // Retrieve the selected event
                var raceEvent =
                    await _apiService.GetEventByIdAsync(eventId);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Identify the logged-in Organiser
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int organiserId))
                {
                    return Challenge();
                }

                // Verify event ownership
                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                // Retrieve the event's categories
                var categories =
                    await _apiService.GetEventCategoriesAsync(eventId);

                // Find the selected category
                var category = categories.FirstOrDefault(
                    c => c.CategoryId == categoryId);

                if (category == null)
                {
                    return NotFound();
                }

                category.EventId = eventId;
                ViewBag.EventName = raceEvent.Name;

                return View(category);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to load category {CategoryId}",
                    categoryId);

                TempData["ErrorMessage"] =
                    "Unable to load category information.";

                return RedirectToAction(
                    "Categories", new { eventId });
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid category response.");

                TempData["ErrorMessage"] =
                    "Unable to process category information.";

                return RedirectToAction(
                    "Categories", new { eventId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(
            int eventId,
            int categoryId,
            CategoryViewModel model)
        {
            if (eventId <= 0 || categoryId <= 0)
            {
                return BadRequest();
            }

            model.EventId = eventId;
            model.CategoryId = categoryId;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Update category through RaceDay API
                using var response =
                    await _apiService.UpdateCategoryAsync(
                        categoryId, model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Category updated successfully!";

                    return RedirectToAction(
                        "Categories", new { eventId });
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
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        nameof(model.Name),
                        "A category with this name already exists.");

                    return View(model);
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid category details.");

                    return View(model);
                }

                ModelState.AddModelError(
                    "",
                    "Unable to update category. Please try again.");

                return View(model);
            }
            catch (InvalidOperationException)
            {
                return Challenge();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Unable to update category {CategoryId}",
                    categoryId);

                ModelState.AddModelError(
                    "",
                    "Unable to connect to RaceDay API.");

                return View(model);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Category update timed out.");

                ModelState.AddModelError(
                    "",
                    "Request timed out. Check the category before retrying.");

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteCategory(
            int eventId, int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
            {
                return BadRequest();
            }

            try
            {
                // Retrieve the event
                var raceEvent =
                    await _apiService.GetEventByIdAsync(eventId);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Identify the logged-in Organiser
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int organiserId))
                {
                    return Challenge();
                }

                // Verify event ownership
                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                // Retrieve the event's categories
                var categories =
                    await _apiService.GetEventCategoriesAsync(eventId);

                // Find the category to delete
                var category = categories.FirstOrDefault(
                    c => c.CategoryId == categoryId);

                if (category == null)
                {
                    return NotFound();
                }

                category.EventId = eventId;

                ViewBag.EventName = raceEvent.Name;

                return View(category);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load category {CategoryId}",
                    categoryId);

                TempData["ErrorMessage"] =
                    "Unable to load category information.";

                return RedirectToAction(
                    "Categories", new { eventId });
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid category response for {CategoryId}",
                    categoryId);

                TempData["ErrorMessage"] =
                    "Unable to process category information.";

                return RedirectToAction(
                    "Categories", new { eventId });
            }
        }


        [HttpPost]
        [ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(
            int eventId, int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
            {
                return BadRequest();
            }

            try
            {
                // Retrieve the event
                var raceEvent =
                    await _apiService.GetEventByIdAsync(eventId);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Identify the logged-in Organiser
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int organiserId))
                {
                    return Challenge();
                }

                // Verify ownership
                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                // Verify the category belongs to this event
                var categories =
                    await _apiService.GetEventCategoriesAsync(eventId);

                var categoryExists = categories.Any(
                    c => c.CategoryId == categoryId);

                if (!categoryExists)
                {
                    return NotFound();
                }

                // Request deletion through the API
                using var response =
                    await _apiService.DeleteCategoryAsync(categoryId);

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NoContent)
                {
                    TempData["SuccessMessage"] =
                        "Category deleted successfully!";

                    return RedirectToAction(
                        "Categories", new { eventId });
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    TempData["ErrorMessage"] =
                        "This category cannot be deleted because " +
                        "participants are already enrolled.";

                    return RedirectToAction(
                        "Categories", new { eventId });
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
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] =
                    "Unable to delete category.";

                return RedirectToAction(
                    "Categories", new { eventId });
            }
            catch (InvalidOperationException)
            {
                return Challenge();
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Category deletion timed out.");

                TempData["ErrorMessage"] =
                    "Request timed out. Check whether the " +
                    "category still exists before retrying.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Category deletion failed.");

                TempData["ErrorMessage"] =
                    "Unable to connect to RaceDay API.";
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid category response.");

                TempData["ErrorMessage"] =
                    "Unable to process category information.";
            }

            return RedirectToAction(
                "Categories", new { eventId });
        }

        [HttpGet]
        public async Task<IActionResult> Enrolments(int eventId)
        {
            if (eventId <= 0)
            {
                return BadRequest();
            }

            // Get the logged-in Organiser's ID
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId))
            {
                return Challenge();
            }

            try
            {
                // Retrieve event information
                var raceEvent =
                    await _apiService.GetEventByIdAsync(eventId);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                // Verify that the Organiser owns this event
                if (raceEvent.OrganiserId != organiserId)
                {
                    return Forbid();
                }

                // Retrieve participants from the API
                var enrolments =
                    await _apiService.GetEventEnrolmentsAsync(eventId);

                // Pass event information to the Razor View
                ViewBag.EventId = eventId;
                ViewBag.EventName = raceEvent.Name;
                ViewBag.TotalEnrolments = enrolments.Count;

                // Order participants by enrolment date
                var sortedEnrolments = enrolments
                    .OrderByDescending(e => e.EnrolmentDate)
                    .ToList();

                return View(sortedEnrolments);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve enrolments for event {EventId}",
                    eventId);

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

                if (ex.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] =
                    "Unable to load event enrolments.";

                return RedirectToAction("Events");
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid enrolments response for event {EventId}",
                    eventId);

                TempData["ErrorMessage"] =
                    "Unable to process enrolment information.";

                return RedirectToAction("Events");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Enrolments request timed out for event {EventId}",
                    eventId);

                TempData["ErrorMessage"] =
                    "The request timed out. Please try again.";

                return RedirectToAction("Events");
            }
            catch (InvalidOperationException)
            {
                return Challenge();
            }
        }


    }
}
