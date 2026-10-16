using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users/me")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

    /// <summary>Gets the logged-in user's own profile.</summary>
    /// <remarks>Any authenticated user. Returns 200 with profile details; 401 if not logged in.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == User.GetUserId());
        return user == null ? Unauthorized() : Ok(UserResponse.From(user));
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <remarks>
    /// Any authenticated user. Expects first name, last name and optional phone number.
    /// Returns 200 with the updated profile; 400 for validation errors; 401 if not logged in.
    /// </remarks>
    [HttpPut]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateProfileRequest request)
    {
        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == User.GetUserId());
        if (user == null) return Unauthorized();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();
        return Ok(UserResponse.From(user));
    }
}
