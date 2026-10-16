using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enters an event under a chosen category (Participant only).</summary>
    /// <remarks>
    /// Expects the id of one of the event's categories. Records the link between the participant, the event and
    /// the category. Returns 201 with the enrolment; 400 for validation errors or a category from another event;
    /// 401 if not logged in; 403 if not a Participant; 404 if the event does not exist;
    /// 409 if already enrolled in this event.
    /// </remarks>
    [HttpPost("events/{eventId:int}/enrolments")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int eventId, EnrolRequest request)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "That category does not belong to this event.");
            return ValidationProblem(ModelState);
        }

        var userId = User.GetUserId();
        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == userId && e.EventId == eventId))
            return Problem(title: "You are already enrolled in this event", statusCode: StatusCodes.Status409Conflict);

        var enrolment = new Enrolment
        {
            ParticipantId = userId,
            EventId = eventId,
            CategoryId = request.CategoryId!.Value,
            EnrolmentDate = DateTime.UtcNow
        };
        _db.Enrolments.Add(enrolment);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Problem(title: "You are already enrolled in this event", statusCode: StatusCodes.Status409Conflict);
        }

        var saved = await _db.Enrolments.WithDetails().AsNoTracking()
            .SingleAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created("/api/enrolments/me", EnrolmentResponse.From(saved));
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <remarks>Returns 200 with the enrolments; 401 if not logged in; 403 if not a Participant.</remarks>
    [HttpGet("enrolments/me")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetMine()
    {
        var userId = User.GetUserId();
        var enrolments = await _db.Enrolments.WithDetails().AsNoTracking()
            .Where(e => e.ParticipantId == userId).OrderBy(e => e.EnrolmentDate).ToListAsync();
        return Ok(enrolments.Select(EnrolmentResponse.From));
    }

    /// <summary>Lists everyone enrolled in an event (owning Organiser only).</summary>
    /// <remarks>
    /// Returns 200 with the enrolments; 401 if not logged in; 403 if not an Organiser or not the event's owner;
    /// 404 if the event does not exist.
    /// </remarks>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var enrolments = await _db.Enrolments.WithDetails().AsNoTracking()
            .Where(e => e.EventId == eventId).OrderBy(e => e.EnrolmentDate).ToListAsync();
        return Ok(enrolments.Select(EnrolmentResponse.From));
    }

    /// <summary>Withdraws from an event (owning Participant only).</summary>
    /// <remarks>
    /// Returns 204 on success; 401 if not logged in; 403 if not a Participant or not your enrolment;
    /// 404 if not found; 409 if a result has already been captured for this enrolment.
    /// </remarks>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var enrolment = await _db.Enrolments.SingleOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return NotFound();
        if (enrolment.ParticipantId != User.GetUserId()) return Forbid();

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Problem(title: "A result has already been captured for this enrolment", statusCode: StatusCodes.Status409Conflict);

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}