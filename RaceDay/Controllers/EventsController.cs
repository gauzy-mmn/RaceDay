
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Services;
using System.Net.Http;

namespace RaceDay.Controllers;

[Route("Events")]
public class EventsController : Controller
{
    private readonly RaceDayApiService _apiService;

    public EventsController(RaceDayApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet("Banner/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Banner(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        try
        {
            // Retrieve event banner from RaceDay API
            using var response =
                await _apiService.GetEventBannerAsync(id);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(502);
            }

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
                return StatusCode(502);
            }

            var imageBytes = await response.Content
                .ReadAsByteArrayAsync();

            Response.Headers["X-Content-Type-Options"] =
                "nosniff";

            Response.Headers.CacheControl = "no-store";

            return File(imageBytes, contentType);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504);
        }
    }
}
