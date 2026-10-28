using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using RaceDay.Services;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

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

    }
}
