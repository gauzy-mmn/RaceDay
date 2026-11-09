using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all events.</summary>
    /// <remarks>Public. Returns 200 with every event, soonest first.</remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetAll()
    {
        var events = await _db.Events.AsNoTracking().OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(EventResponse.From));
    }

    /// <summary>Gets one event.</summary>
    /// <remarks>Public. Returns 200 with the event; 404 if it does not exist.</remarks>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == id);
        return ev == null ? NotFound() : Ok(EventResponse.From(ev));
    }

    /// <summary>Creates an event (Organiser only).</summary>
    /// <remarks>
    /// Expects name, optional description, event date, location, distance in km and event type (Run, Walk or Cycle).
    /// The logged-in Organiser becomes the event's owner.
    /// Returns 201 with the event; 400 for validation errors; 401 if not logged in; 403 if not an Organiser.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> Create(EventRequest request)
    {
        var ev = new RaceEvent
        {
            OrganiserId = User.GetUserId(),
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            Distance = request.Distance!.Value,
            EventType = request.EventType,
            CreatedAt = DateTime.UtcNow
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, EventResponse.From(ev));
    }

    /// <summary>Updates an event (owning Organiser only).</summary>
    /// <remarks>
    /// Expects the same fields as creating an event.
    /// Returns 200 with the updated event; 400 for validation errors; 401 if not logged in;
    /// 403 if not an Organiser or not the event's owner; 404 if the event does not exist.
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.Distance = request.Distance!.Value;
        ev.EventType = request.EventType;
        await _db.SaveChangesAsync();
        return Ok(EventResponse.From(ev));
    }

    /// <summary>Deletes an event (owning Organiser only).</summary>
    /// <remarks>
    /// Also removes the event's categories, enrolments and results.
    /// Returns 204 on success; 401 if not logged in; 403 if not an Organiser or not the owner; 404 if not found.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        // The schema uses NO ACTION foreign keys, so child rows are removed explicitly.
        var enrolments = await _db.Enrolments.Where(e => e.EventId == id).ToListAsync();
        var enrolmentIds = enrolments.Select(e => e.EnrolmentId).ToList();
        var results = await _db.Results.Where(r => enrolmentIds.Contains(r.EnrolmentId)).ToListAsync();
        var categories = await _db.Categories.Where(c => c.EventId == id).ToListAsync();

        _db.Results.RemoveRange(results);
        _db.Enrolments.RemoveRange(enrolments);
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }


    [HttpPost("/api/events/{eventId:int}/banner")]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadEventBanner(
        int eventId,
        IFormFile file,
        [FromServices] AzureBlobStorageService storage,
        [FromServices] ILogger<EventsController> logger)
    {
        // Find the event
        var raceEvent = await _db.Events
            .SingleOrDefaultAsync(e => e.EventId == eventId);

        if (raceEvent == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        // Only the event owner can replace its banner
        if (raceEvent.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        // Validate the selected file
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select an event banner."
            });
        }

        // Remember the previous banner filename
        var oldBlobName = raceEvent.EventBannerBlobName;

        try
        {
            // 1. Upload the replacement banner
            var newBlobName = await storage.UploadImageAsync(
                file,
                "event-banners",
                HttpContext.RequestAborted);

            // 2. Update the event's banner reference
            raceEvent.EventBannerBlobName = newBlobName;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                // Database save failed.
                // Attempt to delete the newly uploaded image.
                try
                {
                    await storage.DeleteImageAsync(
                        newBlobName,
                        "event-banners",
                        CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    logger.LogWarning(
                        cleanupEx,
                        "Failed to remove orphaned event banner {BlobName}",
                        newBlobName);
                }

                logger.LogError(
                    dbEx,
                    "Failed to save banner for event {EventId}",
                    eventId);

                return StatusCode(500, new
                {
                    message = "Unable to save event banner."
                });
            }

            // 3. Delete the old banner after SQL succeeds
            if (!string.IsNullOrWhiteSpace(oldBlobName) &&
                oldBlobName != newBlobName)
            {
                try
                {
                    await storage.DeleteImageAsync(
                        oldBlobName,
                        "event-banners",
                        CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    // The new banner is already saved.
                    // Log the failure without rejecting the upload.
                    logger.LogWarning(
                        cleanupEx,
                        "Failed to delete previous banner {BlobName}",
                        oldBlobName);
                }
            }

            // 4. Return success
            return Ok(new
            {
                message = "Event banner uploaded successfully.",
                eventId = raceEvent.EventId,
                eventBannerBlobName = newBlobName
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Azure.RequestFailedException ex)
        {
            logger.LogError(
                ex,
                "Azure banner upload failed for event {EventId}",
                eventId);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Image storage is temporarily unavailable."
                });
        }
    }


    // Retrieve an event banner from Azure Blob Storage
    [HttpGet("{eventId:int}/banner")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventBanner(
        int eventId,
        [FromServices] AzureBlobStorageService storage)
    {
        // Find the event in SQL Server
        var raceEvent = await _db.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EventId == eventId);

        if (raceEvent == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        // Check whether the event has a banner
        if (string.IsNullOrWhiteSpace(
            raceEvent.EventBannerBlobName))
        {
            return NotFound(new
            {
                message = "This event has no banner."
            });
        }

        try
        {
            // Retrieve the image from private Blob Storage
            var (content, contentType) =
                await storage.GetEventBannerAsync(
                    raceEvent.EventBannerBlobName,
                    HttpContext.RequestAborted);

            // Validate the returned image type
            if (contentType != "image/jpeg" &&
                contentType != "image/png" &&
                contentType != "image/webp")
            {
                await content.DisposeAsync();

                return StatusCode(502, new
                {
                    message = "Invalid banner content type."
                });
            }

            // Prevent browser MIME-type sniffing
            Response.Headers["X-Content-Type-Options"] =
                "nosniff";

            // Return the image to the browser
            return File(content, contentType);
        }
        catch (Azure.RequestFailedException ex)
            when (ex.Status == 404)
        {
            return NotFound(new
            {
                message = "Banner image not found in storage."
            });
        }
        catch (Azure.RequestFailedException)
        {
            return StatusCode(503, new
            {
                message = "Banner storage is temporarily unavailable."
            });
        }
    }

}