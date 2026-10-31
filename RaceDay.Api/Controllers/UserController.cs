using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Services;

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


    [HttpPost("/api/users/me/profile-picture")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile file,
        [FromServices] AzureBlobStorageService storage,
        [FromServices] ILogger<UsersController> logger)
    {
        // Identify the currently authenticated user
        var userId = User.GetUserId();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return Unauthorized();
        }

        // Validate the selected file
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select a profile picture."
            });
        }

        // Remember the previous image
        var oldBlobName = user.ProfilePictureBlobName;

        try
        {
            // 1. Upload the replacement image
            var newBlobName = await storage.UploadImageAsync(
                file,
                "profile-pictures",
                HttpContext.RequestAborted);

            // 2. Save the new image filename
            user.ProfilePictureBlobName = newBlobName;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                // Database save failed.
                // Attempt to remove the newly uploaded image.
                try
                {
                    await storage.DeleteImageAsync(
                        newBlobName,
                        "profile-pictures",
                        CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    logger.LogWarning(
                        cleanupEx,
                        "Failed to remove orphaned profile picture {BlobName}",
                        newBlobName);
                }

                logger.LogError(
                    dbEx,
                    "Failed to save profile picture for user {UserId}",
                    userId);

                return StatusCode(500, new
                {
                    message = "Unable to save profile picture."
                });
            }

            // 3. Delete the old image only after SQL succeeds
            if (!string.IsNullOrWhiteSpace(oldBlobName) &&
                oldBlobName != newBlobName)
            {
                try
                {
                    await storage.DeleteImageAsync(
                        oldBlobName,
                        "profile-pictures",
                        CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    // The new image is already saved.
                    // Don't turn a successful upload into a failure.
                    logger.LogWarning(
                        cleanupEx,
                        "Failed to delete previous profile picture {BlobName}",
                        oldBlobName);
                }
            }

            // 4. Return success
            return Ok(new
            {
                message = "Profile picture uploaded successfully.",
                profilePictureBlobName = newBlobName
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
                "Azure profile picture upload failed.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Image storage is temporarily unavailable."
                });
        }
    }





    [HttpGet("profile-picture")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetProfilePicture(
        [FromServices] AzureBlobStorageService storage)
    {
        // Identify the authenticated user
        var userId = User.GetUserId();

        // Retrieve the user's profile from SQL Server
        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return Unauthorized();
        }

        // Check whether the user has uploaded a picture
        if (string.IsNullOrWhiteSpace(
            user.ProfilePictureBlobName))
        {
            return NotFound(new
            {
                message = "No profile picture has been uploaded."
            });
        }

        try
        {
            // Retrieve the image from private Blob Storage
            var image = await storage.GetProfilePictureAsync(
                user.ProfilePictureBlobName,
                HttpContext.RequestAborted);

            // Prevent caching of private profile images
            Response.Headers.CacheControl = "private, no-store";

            // Return the image directly to the browser
            return File(
                image.Content,
                image.ContentType);
        }
        catch (Azure.RequestFailedException ex)
            when (ex.Status == 404)
        {
            return NotFound(new
            {
                message = "Profile picture was not found in storage."
            });
        }
        catch (Azure.RequestFailedException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Image storage is temporarily unavailable."
                });
        }
    }


}
