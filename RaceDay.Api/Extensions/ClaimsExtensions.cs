using System.Security.Claims;
namespace RaceDay.Api.Extensions
{
    public static class ClaimsExtensions
    {
        /// <summary>The logged-in user's id, taken from the session cookie's claims.</summary>
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
