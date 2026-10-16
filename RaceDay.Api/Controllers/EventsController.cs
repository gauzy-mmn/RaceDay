using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

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
}