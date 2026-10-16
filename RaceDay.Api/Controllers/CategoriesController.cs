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
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists the categories of an event.</summary>
    /// <remarks>Public. Returns 200 with the event's categories; 404 if the event does not exist.</remarks>
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId).OrderBy(c => c.Name).ToListAsync();
        return Ok(categories.Select(CategoryResponse.From));
    }

    /// <summary>Adds a category to an event (owning Organiser only).</summary>
    /// <remarks>
    /// Expects a name (unique within the event) and optional description, e.g. "Under 20" or "21km".
    /// Returns 201 with the category; 400 for validation errors; 401 if not logged in;
    /// 403 if not an Organiser or not the event's owner; 404 if the event does not exist; 409 if the name is taken.
    /// </remarks>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
            return Problem(title: "This event already has a category with that name", statusCode: StatusCodes.Status409Conflict);

        var category = new Category { EventId = eventId, Name = name, Description = request.Description };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", CategoryResponse.From(category));
    }

    /// <summary>Updates a category (owning Organiser only).</summary>
    /// <remarks>
    /// Expects a name and optional description.
    /// Returns 200 with the updated category; 400 for validation errors; 401 if not logged in;
    /// 403 if not an Organiser or not the owner; 404 if not found; 409 if the name is taken within the event.
    /// </remarks>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();
        if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.Name == name && c.CategoryId != id))
            return Problem(title: "This event already has a category with that name", statusCode: StatusCodes.Status409Conflict);

        category.Name = name;
        category.Description = request.Description;
        await _db.SaveChangesAsync();
        return Ok(CategoryResponse.From(category));
    }

    /// <summary>Deletes a category (owning Organiser only).</summary>
    /// <remarks>
    /// Returns 204 on success; 401 if not logged in; 403 if not an Organiser or not the owner; 404 if not found;
    /// 409 if participants are already enrolled in the category.
    /// </remarks>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();
        if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Problem(title: "Participants are already enrolled in this category", statusCode: StatusCodes.Status409Conflict);

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}