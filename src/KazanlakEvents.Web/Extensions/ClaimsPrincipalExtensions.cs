using System.Security.Claims;

namespace KazanlakEvents.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Admin-tier accounts are excluded from community participation (registration, favorites, comments, volunteering)
    public static bool CanParticipate(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true &&
        !user.IsInRole("Admin") &&
        !user.IsInRole("SuperAdmin") &&
        !user.IsInRole("Moderator") &&
        !user.IsInRole("BlogAuthor");
}
