
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly RaceDayApiService _apiService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            RaceDayApiService apiService,
            ILogger<ProfileController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                // Retrieve the user's existing profile
                using var response =
                    await _apiService.GetMyProfileAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load your profile.";

                    return RedirectToAction("Index", "Home");
                }

                // Read the profile information from the API
                var profile = await response.Content
                    .ReadFromJsonAsync<UserProfileViewModel>();

                if (profile == null)
                {
                    TempData["ErrorMessage"] =
                        "Your profile information could not be loaded.";

                    return RedirectToAction("Index", "Home");
                }

                // Populate the editing form
                var model = new UpdateProfileViewModel
                {
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    PhoneNumber = profile.PhoneNumber
                };

                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Failed to retrieve user profile.");

                TempData["ErrorMessage"] =
                    "Unable to connect to the RaceDay API.";
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid user profile response.");

                TempData["ErrorMessage"] =
                    "Unable to process your profile information.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "Profile request timed out.");

                TempData["ErrorMessage"] =
                    "The request timed out. Please try again.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex, "API authentication session unavailable.");

                return Challenge();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            UpdateProfileViewModel model)
        {
            // Check the form validation
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Send updated details to RaceDay API
                using var response =
                    await _apiService.UpdateMyProfileAsync(model);

                // Profile updated successfully
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Your profile has been updated successfully.";

                    return RedirectToAction(nameof(Edit));
                }

                // Handle expired authentication
                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                // Handle API validation errors
                if (response.StatusCode ==
                    HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "The API rejected your details. " +
                        "Please check your information and try again.");

                    return View(model);
                }

                // Handle other errors
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update your profile. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user profile.");

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to connect to the RaceDay API.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Profile update request timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "The request timed out. Check whether your changes " +
                    "were saved before trying again.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "API authentication session unavailable.");

                return Challenge();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile file)
        {
            // Check that a file was selected
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a profile picture.";

                return RedirectToAction(nameof(Edit));
            }

            // Restrict file size to 5 MB
            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                TempData["ErrorMessage"] =
                    "Profile picture must not exceed 5 MB.";

                return RedirectToAction(nameof(Edit));
            }

            try
            {
                // Send image to the authenticated RaceDay API
                using var response =
                    await _apiService.UploadMyProfilePictureAsync(file);

                // Handle successful upload
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Profile picture uploaded successfully.";

                    return RedirectToAction(nameof(Edit));
                }

                // Handle expired authentication
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                // Handle invalid image uploads
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
                {
                    TempData["ErrorMessage"] =
                        "Invalid image. Please use a valid JPG, PNG " +
                        "or WebP file smaller than 5 MB.";
                }
                else if (response.StatusCode ==
                    System.Net.HttpStatusCode.RequestEntityTooLarge)
                {
                    TempData["ErrorMessage"] =
                        "The selected image is too large.";
                }
                else if (response.StatusCode ==
                    System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    TempData["ErrorMessage"] =
                        "Image storage is unavailable. Please try again.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Unable to upload your profile picture.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to upload profile picture.");

                TempData["ErrorMessage"] =
                    "Unable to connect to the RaceDay API.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Profile picture upload timed out.");

                TempData["ErrorMessage"] =
                    "The upload timed out. Check whether your " +
                    "picture was saved before trying again.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "API authentication session unavailable.");

                return Challenge();
            }

            return RedirectToAction(nameof(Edit));
        }

        [HttpGet]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Picture()
        {
            try
            {
                // Retrieve the picture through RaceDay API
                using var response =
                    await _apiService.GetMyProfilePictureAsync();

                // No profile picture has been uploaded
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                // API authentication has expired
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return Unauthorized();
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                // Handle unsuccessful API responses
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Profile picture API returned status {StatusCode}",
                        response.StatusCode);

                    return StatusCode(502);
                }

                // Retrieve the image content type
                var contentType = response.Content.Headers
                    .ContentType?.MediaType;

                var allowedTypes = new[]
                {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

                if (contentType == null ||
                    !allowedTypes.Contains(
                        contentType,
                        StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Unexpected profile picture content type: {ContentType}",
                        contentType);

                    return StatusCode(502);
                }

                // Read image bytes before disposing API response
                var imageBytes = await response.Content
                    .ReadAsByteArrayAsync();

                // Prevent private images from being cached
                Response.Headers.CacheControl = "private, no-store";

                // Send the image to the browser
                return File(imageBytes, contentType);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve profile picture.");

                return StatusCode(503);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Profile picture request timed out.");

                return StatusCode(504);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "API authentication session unavailable.");

                return Unauthorized();
            }
        }

    }


}
