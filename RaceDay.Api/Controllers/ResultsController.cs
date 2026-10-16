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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Captures a participant's finish time and position (owning Organiser only).</summary>
    /// <remarks>
    /// Expects an enrolment id (which must belong to this event), a finish time (hh:mm:ss) and a finish position.
    /// Returns 201 with the result; 400 for validation errors or an enrolment from another event;
    /// 401 if not logged in; 403 if not an Organiser or not the event's owner; 404 if the event does not exist;
    /// 409 if a result was already captured for that enrolment.
    /// </remarks>
    [HttpPost("events/{eventId:int}/results")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> Create(int eventId, CreateResultRequest request)
    {
        var ev = await _db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        if (request.FinishTime!.Value <= TimeSpan.Zero)
        {
            ModelState.AddModelError(nameof(request.FinishTime), "Finish time must be greater than zero.");
            return ValidationProblem(ModelState);
        }

        if (!await _db.Enrolments.AnyAsync(e => e.EnrolmentId == request.EnrolmentId && e.EventId == eventId))
        {
            ModelState.AddModelError(nameof(request.EnrolmentId), "That enrolment does not belong to this event.");
            return ValidationProblem(ModelState);
        }

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Problem(title: "A result has already been captured for this enrolment", statusCode: StatusCodes.Status409Conflict);

        var result = new RaceResult
        {
            EnrolmentId = request.EnrolmentId!.Value,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition,
            CapturedAt = DateTime.UtcNow
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await _db.Results.WithDetails().AsNoTracking().SingleAsync(r => r.ResultId == result.ResultId);
        return Created($"/api/events/{eventId}/results", ResultResponse.From(saved));
    }

    /// <summary>Corrects a captured result (owning Organiser only).</summary>
    /// <remarks>
    /// Expects a finish time (hh:mm:ss) and a finish position.
    /// Returns 200 with the updated result; 400 for validation errors; 401 if not logged in;
    /// 403 if not an Organiser or not the event's owner; 404 if the result does not exist.
    /// </remarks>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> Update(int id, UpdateResultRequest request)
    {
        var result = await _db.Results.WithDetails().SingleOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();
        if (result.Enrolment.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (request.FinishTime!.Value <= TimeSpan.Zero)
        {
            ModelState.AddModelError(nameof(request.FinishTime), "Finish time must be greater than zero.");
            return ValidationProblem(ModelState);
        }

        result.FinishTime = request.FinishTime;
        result.FinishPosition = request.FinishPosition;
        await _db.SaveChangesAsync();
        return Ok(ResultResponse.From(result));
    }

    /// <summary>Lists the logged-in Participant's own results.</summary>
    /// <remarks>Returns 200 with the participant's results across events; 401 if not logged in; 403 if not a Participant.</remarks>
    [HttpGet("results/me")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetMine()
    {
        var userId = User.GetUserId();
        var results = await _db.Results.WithDetails().AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == userId).OrderBy(r => r.CapturedAt).ToListAsync();
        return Ok(results.Select(ResultResponse.From));
    }

    /// <summary>Lists all results for an event (owning Organiser only).</summary>
    /// <remarks>
    /// Returns 200 with the results ordered by finish position; 401 if not logged in;
    /// 403 if not an Organiser or not the event's owner; 404 if the event does not exist.
    /// </remarks>
    [HttpGet("events/{eventId:int}/results")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var results = await _db.Results.WithDetails().AsNoTracking()
            .Where(r => r.Enrolment.EventId == eventId).OrderBy(r => r.FinishPosition).ToListAsync();
        return Ok(results.Select(ResultResponse.From));
    }
}