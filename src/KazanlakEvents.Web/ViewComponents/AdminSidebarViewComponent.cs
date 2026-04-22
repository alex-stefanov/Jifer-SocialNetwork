using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Web.ViewComponents;

public class AdminSidebarViewComponent(IApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var pendingEvents         = await db.Events.CountAsync(e => e.Status == EventStatus.PendingApproval);
        var pendingOrgReqs        = await db.OrganizerRequests.CountAsync(r => !r.IsReviewed);
        var pendingVenueReqs      = await db.VenueRequests.CountAsync(r => r.Status == VenueRequestStatus.Pending);
        var pendingBlogAuthorReqs = await db.BlogAuthorRequests.CountAsync(r => !r.IsReviewed);

        ViewBag.PendingEvents            = pendingEvents;
        ViewBag.PendingOrganizerRequests = pendingOrgReqs;
        ViewBag.PendingVenueRequests     = pendingVenueReqs;
        ViewBag.PendingBlogAuthorRequests = pendingBlogAuthorReqs;
        ViewBag.ActivePage     = ViewContext.ViewData["AdminPage"] as string ?? string.Empty;

        var userName = UserClaimsPrincipal.Identity?.Name ?? string.Empty;
        var parts    = userName.Split(new[] { ' ', '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
        ViewBag.Initials  = parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0
            ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}"
            : userName.Length > 0 ? char.ToUpper(userName[0]).ToString() : "?";
        ViewBag.UserName    = userName;
        ViewBag.IsSuperAdmin = UserClaimsPrincipal.IsInRole("SuperAdmin");
        ViewBag.IsAdmin      = UserClaimsPrincipal.IsInRole("Admin") || UserClaimsPrincipal.IsInRole("SuperAdmin");
        ViewBag.IsModerator  = UserClaimsPrincipal.IsInRole("Moderator");

        return View();
    }
}
