using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace RaceDay.Controllers
{
    public class AccountController : Controller
    {
        private readonly RaceDayApiService _apiService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            RaceDayApiService apiService,
            ILogger<AccountController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Display the login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Process the login form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using var response =
                    await _apiService.LoginAsync(model);

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid email or password.");

                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "",
                        "Login failed. Please try again.");

                    return View(model);
                }

                var user = await response.Content
                    .ReadFromJsonAsync<LoginResponse>();

                if (user == null ||
                    user.UserId <= 0 ||
                    string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid response from the server.");

                    return View(model);
                }

                // Validate and normalise the user's role
                string role;

                if (string.Equals(
                    user.Role, "Organiser",
                    StringComparison.OrdinalIgnoreCase))
                {
                    role = "Organiser";
                }
                else if (string.Equals(
                    user.Role, "Participant",
                    StringComparison.OrdinalIgnoreCase))
                {
                    role = "Participant";
                }
                else
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid account role.");

                    return View(model);
                }

                // Get authentication cookies issued by API
                if (!response.Headers.TryGetValues(
                    "Set-Cookie", out var cookieHeaders))
                {
                    ModelState.AddModelError(
                        "",
                        "Authentication session could not be established.");

                    return View(model);
                }

                var apiCookies = cookieHeaders
                    .Select(cookie => cookie.Split(';', 2)[0])
                    .Where(cookie =>
                        !string.IsNullOrWhiteSpace(cookie) &&
                        cookie.Contains('='))
                    .ToArray();

                if (apiCookies.Length == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Authentication cookie was not received.");

                    return View(model);
                }

                // Store API cookies on the server-side session
                HttpContext.Session.Clear();

                HttpContext.Session.SetString(
                    "ApiAuthCookie",
                    string.Join("; ", apiCookies));

                // Create MVC authentication claims
                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.UserId.ToString()),

                    new Claim(
                        ClaimTypes.Email,
                        user.Email),

                    new Claim(
                        ClaimTypes.Role,
                        role)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                // Sign the user into the MVC application
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false
                    });

                // Temporary redirect until dashboards exist
                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "RaceDay API login request failed.");

                ModelState.AddModelError(
                    "",
                    "Unable to connect to the server.");

                return View(model);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(
                    ex, "Invalid RaceDay API login response.");

                ModelState.AddModelError(
                    "",
                    "Unable to process the login response.");

                return View(model);
            }
        }

        // Display registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using var response =
                    await _apiService.RegisterAsync(model);

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Created)
                {
                    TempData["SuccessMessage"] =
                        "Account created successfully! Please log in.";

                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "An account with this email already exists.");

                    return View(model);
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid registration details. Please check your information.");

                    return View(model);
                }

                ModelState.AddModelError(
                    "",
                    "Registration failed. Please try again.");

                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "RaceDay API registration request failed.");

                ModelState.AddModelError(
                    "",
                    "Unable to connect to the server.");

                return View(model);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "RaceDay registration request timed out.");

                ModelState.AddModelError(
                    "",
                    "The request timed out. Please try again.");

                return View(model);
            }
        }

        // Temporary access-denied response
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content("Access denied.");
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult CheckLogin()
        {
            var isLoggedIn =
                User.Identity?.IsAuthenticated ?? false;

            var role = User.FindFirst(
                System.Security.Claims.ClaimTypes.Role)?.Value;

            return Content(
                $"Logged In: {isLoggedIn}\n" +
                $"Role: {role ?? "Unknown"}");
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> CheckApiProfile()
        {
            try
            {
                using var response =
                    await _apiService.GetMyProfileAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(
                        "Success! MVC is authenticated with the RaceDay API.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Unauthorized(
                        "The API did not recognise your login session.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return StatusCode(403,
                        "The API denied access to this profile.");
                }

                return StatusCode(502,
                    "The API could not retrieve your profile.");
            }
            catch (InvalidOperationException)
            {
                return Unauthorized(
                    "Your API authentication session is missing.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API profile request failed.");

                return StatusCode(503,
                    "Unable to connect to RaceDay API.");
            }
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Log out from the RaceDay API
                using var response = await _apiService.LogoutAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "RaceDay API logout returned status {StatusCode}",
                        response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex, "Unable to connect to the API during logout.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    ex, "API logout request timed out.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex, "API authentication session was unavailable.");
            }
            finally
            {
                // Clear all session data
                HttpContext.Session.Clear();

                // Sign out from MVC authentication
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }

            // Redirect user to Login page
            return RedirectToAction("Login", "Account");
        }

    }



}
