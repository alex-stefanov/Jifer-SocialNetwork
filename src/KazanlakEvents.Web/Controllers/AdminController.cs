using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.Resources;
using KazanlakEvents.Web.ViewModels.Admin;
using KazanlakEvents.Web.ViewModels.Sponsor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace KazanlakEvents.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin,Moderator")]
public class AdminController(
    IAdminService adminService,
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<SharedResource> localizer,
    IBlogService blogService,
    ISponsorService sponsorService,
    IFileStorageService fileStorage) : Controller
{
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> Dashboard(CancellationToken ct = default)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return RedirectToAction(nameof(ModeratorDashboard));
        return await DashboardInternal(ct);
    }

    private async Task<IActionResult> DashboardInternal(CancellationToken ct)
    {
        var now            = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevMonthStart = thisMonthStart.AddMonths(-1);
        var chartCutoff    = thisMonthStart.AddMonths(-11);

        var totalUsersResult   = await adminService.GetTotalUsersCountAsync(ct);
        var totalTicketsResult = await adminService.GetTotalTicketsSoldAsync(ct);
        var pendingApprovalsResult = await adminService.GetPendingApprovalsCountAsync(ct);

        var activeEventsResult = await db.Events
            .Where(e => e.Status == Domain.Enums.EventStatus.Published
                     || e.Status == Domain.Enums.EventStatus.Ongoing)
            .CountAsync(ct);

        var usersCurrentMonth = await userManager.Users
            .CountAsync(u => u.CreatedAt >= thisMonthStart, ct);
        var usersPrevMonth = await userManager.Users
            .CountAsync(u => u.CreatedAt >= prevMonthStart && u.CreatedAt < thisMonthStart, ct);

        var activeEventsCurrentMonth = await db.Events
            .CountAsync(e => (e.Status == Domain.Enums.EventStatus.Published
                           || e.Status == Domain.Enums.EventStatus.Ongoing)
                           && e.CreatedAt >= thisMonthStart, ct);
        var activeEventsPrevMonth = await db.Events
            .CountAsync(e => (e.Status == Domain.Enums.EventStatus.Published
                           || e.Status == Domain.Enums.EventStatus.Ongoing)
                           && e.CreatedAt >= prevMonthStart && e.CreatedAt < thisMonthStart, ct);

        var ticketsCurrentMonth = await db.Tickets
            .CountAsync(t => t.IssuedAt >= thisMonthStart, ct);
        var ticketsPrevMonth = await db.Tickets
            .CountAsync(t => t.IssuedAt >= prevMonthStart && t.IssuedAt < thisMonthStart, ct);

        var monthLabels = Enumerable.Range(0, 12)
            .Select(i => chartCutoff.AddMonths(i).ToString("MMM yyyy"))
            .ToList();

        var eventsRaw = await db.Events
            .Where(e => e.CreatedAt >= chartCutoff)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var eventsPerMonth = Enumerable.Range(0, 12).Select(i =>
        {
            var d = chartCutoff.AddMonths(i);
            return eventsRaw.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0;
        }).ToList();

        var recentEvents = await db.Events
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new RecentEventItem
            {
                Id        = e.Id,
                Title     = e.Title,
                Slug      = e.Slug,
                CreatedAt = e.CreatedAt,
                Status    = e.Status
            })
            .ToListAsync(ct);

        return View(new DashboardViewModel
        {
            TotalUsers               = totalUsersResult,
            ActiveEvents             = activeEventsResult,
            TotalRegistrations       = totalTicketsResult,
            PendingApprovals         = pendingApprovalsResult,
            UsersCurrentMonth        = usersCurrentMonth,
            UsersPrevMonth           = usersPrevMonth,
            ActiveEventsCurrentMonth = activeEventsCurrentMonth,
            ActiveEventsPrevMonth    = activeEventsPrevMonth,
            TicketsCurrentMonth      = ticketsCurrentMonth,
            TicketsPrevMonth         = ticketsPrevMonth,
            MonthLabels              = monthLabels,
            EventsPerMonth           = eventsPerMonth,
            RecentEvents             = recentEvents
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AuditLogs(
        int page = 1,
        DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default)
    {
        const int pageSize = 20;

        var logs  = await adminService.GetAuditLogsAsync(page, pageSize, null, null, from, to, ct);
        var count = await adminService.GetAuditLogsTotalCountAsync(null, null, from, to, ct);

        var userIds = logs
            .Where(l => l.UserId.HasValue)
            .Select(l => l.UserId!.Value)
            .Distinct()
            .ToList();

        var userNames = new Dictionary<Guid, string>();
        foreach (var uid in userIds)
        {
            var user = await userManager.FindByIdAsync(uid.ToString());
            if (user != null)
                userNames[uid] = user.UserName!;
        }

        var vm = new AuditLogListViewModel
        {
            CurrentPage = page,
            TotalPages  = (int)Math.Ceiling(count / (double)pageSize),
            FromFilter  = from,
            ToFilter    = to,
            Logs = logs.Select(l => new AuditLogItemViewModel
            {
                Id        = l.Id,
                UserName  = l.UserId.HasValue && userNames.TryGetValue(l.UserId.Value, out var n) ? n : null,
                Action    = l.Action,
                Timestamp = l.Timestamp
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> OrganizerRequests(CancellationToken ct = default)
    {
        ViewData["AdminPage"] = "OrganizerRequests";
        ViewData["AdminPageTitle"] = localizer["OrganizerRequestsPageTitle"].Value;

        var requests = await db.OrganizerRequests
            .OrderBy(r => r.IsReviewed)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var userIds = requests.Select(r => r.UserId).Distinct().ToList();
        var reviewerIds = requests.Where(r => r.ReviewedById.HasValue)
            .Select(r => r.ReviewedById!.Value).Distinct().ToList();

        var allIds = userIds.Concat(reviewerIds).Distinct().ToList();
        var userNames = new Dictionary<Guid, string>();
        foreach (var uid in allIds)
        {
            var u = await userManager.FindByIdAsync(uid.ToString());
            if (u != null) userNames[uid] = u.UserName!;
        }

        var items = requests.Select(r => new OrganizerRequestItemViewModel
        {
            Id           = r.Id,
            UserId       = r.UserId,
            UserName     = userNames.TryGetValue(r.UserId, out var un) ? un : null,
            Reason       = r.Reason,
            IsReviewed   = r.IsReviewed,
            IsApproved   = r.IsApproved,
            ReviewNotes  = r.ReviewNotes,
            CreatedAt    = r.CreatedAt,
            ReviewedAt   = r.ReviewedAt,
            ReviewedByName = r.ReviewedById.HasValue && userNames.TryGetValue(r.ReviewedById.Value, out var rn) ? rn : null
        }).ToList();

        return View(new OrganizerRequestListViewModel
        {
            Requests     = items,
            PendingCount = items.Count(i => !i.IsReviewed)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ApproveOrganizerRequest(
        Guid requestId, CancellationToken ct = default)
    {
        var request = await db.OrganizerRequests.FindAsync(new object[] { requestId }, ct);
        if (request == null) return NotFound();

        var reviewer = await userManager.GetUserAsync(User);
        request.IsApproved  = true;
        request.IsReviewed  = true;
        request.ReviewedById = reviewer!.Id;
        request.ReviewedAt  = DateTime.UtcNow;

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user != null && !await userManager.IsInRoleAsync(user, "Organizer"))
            await userManager.AddToRoleAsync(user, "Organizer");

        await db.SaveChangesAsync(ct);
        TempData["Success"] = localizer["OrganizerRequestApproved"].Value;
        return RedirectToAction(nameof(OrganizerRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DenyOrganizerRequest(
        Guid requestId, string? notes, CancellationToken ct = default)
    {
        var request = await db.OrganizerRequests.FindAsync(new object[] { requestId }, ct);
        if (request == null) return NotFound();

        var reviewer = await userManager.GetUserAsync(User);
        request.IsApproved   = false;
        request.IsReviewed   = true;
        request.ReviewedById  = reviewer!.Id;
        request.ReviewedAt   = DateTime.UtcNow;
        request.ReviewNotes  = notes;

        await db.SaveChangesAsync(ct);
        TempData["Success"] = localizer["OrganizerRequestDenied"].Value;
        return RedirectToAction(nameof(OrganizerRequests));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> BlogAuthorRequests(CancellationToken ct = default)
    {
        ViewData["AdminPage"] = "BlogAuthorRequests";
        ViewData["AdminPageTitle"] = localizer["BlogAuthorRequestsPageTitle"].Value;

        var requests = await db.BlogAuthorRequests
            .OrderBy(r => r.IsReviewed)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var userIds     = requests.Select(r => r.UserId).Distinct().ToList();
        var reviewerIds = requests.Where(r => r.ReviewedById.HasValue)
            .Select(r => r.ReviewedById!.Value).Distinct().ToList();

        var allIds    = userIds.Concat(reviewerIds).Distinct().ToList();
        var userNames = new Dictionary<Guid, string>();
        foreach (var uid in allIds)
        {
            var u = await userManager.FindByIdAsync(uid.ToString());
            if (u != null) userNames[uid] = u.UserName!;
        }

        var items = requests.Select(r => new BlogAuthorRequestItemViewModel
        {
            Id             = r.Id,
            UserId         = r.UserId,
            UserName       = userNames.TryGetValue(r.UserId, out var un) ? un : null,
            Reason         = r.Reason,
            IsReviewed     = r.IsReviewed,
            IsApproved     = r.IsApproved,
            ReviewNotes    = r.ReviewNotes,
            CreatedAt      = r.CreatedAt,
            ReviewedAt     = r.ReviewedAt,
            ReviewedByName = r.ReviewedById.HasValue && userNames.TryGetValue(r.ReviewedById.Value, out var rn) ? rn : null
        }).ToList();

        return View(new BlogAuthorRequestListViewModel
        {
            Requests     = items,
            PendingCount = items.Count(i => !i.IsReviewed)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> ApproveBlogAuthorRequest(
        Guid requestId, CancellationToken ct = default)
    {
        var request = await db.BlogAuthorRequests.FindAsync(new object[] { requestId }, ct);
        if (request == null) return NotFound();

        var reviewer = await userManager.GetUserAsync(User);
        request.IsApproved   = true;
        request.IsReviewed   = true;
        request.ReviewedById = reviewer!.Id;
        request.ReviewedAt   = DateTime.UtcNow;

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user != null && !await userManager.IsInRoleAsync(user, "BlogAuthor"))
            await userManager.AddToRoleAsync(user, "BlogAuthor");

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);
        if (profile != null)
            profile.IsTrustedAuthor = true;

        await db.SaveChangesAsync(ct);
        TempData["Success"] = localizer["BlogAuthorRequestApproved"].Value;
        return RedirectToAction(nameof(BlogAuthorRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> DenyBlogAuthorRequest(
        Guid requestId, string? notes, CancellationToken ct = default)
    {
        var request = await db.BlogAuthorRequests.FindAsync(new object[] { requestId }, ct);
        if (request == null) return NotFound();

        var reviewer = await userManager.GetUserAsync(User);
        request.IsApproved   = false;
        request.IsReviewed   = true;
        request.ReviewedById = reviewer!.Id;
        request.ReviewedAt   = DateTime.UtcNow;
        request.ReviewNotes  = notes;

        await db.SaveChangesAsync(ct);
        TempData["Error"] = localizer["BlogAuthorRequestDenied"].Value;
        return RedirectToAction(nameof(BlogAuthorRequests));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> VenueRequests(CancellationToken ct = default)
    {
        ViewData["AdminPage"] = "VenueRequests";
        ViewData["AdminPageTitle"] = localizer["VenueRequests"].Value;

        var requests = await db.VenueRequests
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var userIds = requests.Select(r => r.RequestedByUserId).Distinct().ToList();
        var userNames = new Dictionary<Guid, string>();
        foreach (var uid in userIds)
        {
            var u = await userManager.FindByIdAsync(uid.ToString());
            if (u != null) userNames[uid] = u.UserName!;
        }

        var items = requests.Select(r => new VenueRequestItemViewModel
        {
            Id          = r.Id,
            Name        = r.Name,
            Address     = r.Address,
            Latitude    = r.Latitude,
            Longitude   = r.Longitude,
            Notes       = r.Notes,
            Status      = r.Status,
            CreatedAt   = r.CreatedAt,
            ReviewNotes = r.ReviewNotes,
            RequestedBy = userNames.TryGetValue(r.RequestedByUserId, out var n) ? n : "?"
        }).ToList();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ApproveVenueRequest(
        Guid requestId, CancellationToken ct = default)
    {
        var req = await db.VenueRequests.FindAsync(new object[] { requestId }, ct);
        if (req == null) return NotFound();

        var reviewer = await userManager.GetUserAsync(User);
        var venue = new Venue
        {
            Name            = req.Name,
            Address         = req.Address,
            City            = "Казанлък",
            Latitude        = req.Latitude ?? 42.6167m,
            Longitude       = req.Longitude ?? 25.4000m,
            CreatedByUserId = req.RequestedByUserId
        };
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        req.Status         = VenueRequestStatus.Approved;
        req.ReviewedById   = reviewer!.Id;
        req.ReviewedAt     = DateTime.UtcNow;
        req.CreatedVenueId = venue.Id;
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["VenueRequestApprovedMsg"].Value;
        return RedirectToAction(nameof(VenueRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RejectVenueRequest(
        Guid requestId, string? notes, CancellationToken ct = default)
    {
        var req = await db.VenueRequests.FindAsync(new object[] { requestId }, ct);
        if (req == null) return NotFound();

        var reviewer = await userManager.GetUserAsync(User);
        req.Status       = VenueRequestStatus.Rejected;
        req.ReviewedById = reviewer!.Id;
        req.ReviewedAt   = DateTime.UtcNow;
        req.ReviewNotes  = notes;
        await db.SaveChangesAsync(ct);

        TempData["Error"] = localizer["VenueRequestRejectedMsg"].Value;
        return RedirectToAction(nameof(VenueRequests));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> ModeratorDashboard(CancellationToken ct = default)
    {
        ViewData["AdminPage"] = "ModeratorDashboard";
        ViewData["AdminPageTitle"] = localizer["ModeratorDashboard"].Value;

        var cutoff = DateTime.UtcNow.AddDays(-30);

        var pendingEventEntities = await db.Events
            .Include(e => e.Category)
            .Where(e => e.Status == EventStatus.PendingApproval)
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var modifiedEventEntities = await db.Events
            .Include(e => e.Category)
            .Where(e => e.ModifiedAt != null && e.ModifiedAt >= cutoff
                     && e.Status != EventStatus.PendingApproval)
            .OrderByDescending(e => e.ModifiedAt)
            .Take(50)
            .ToListAsync(ct);

        var pendingBlogEntities = await db.BlogPosts
            .Where(bp => bp.Status == Domain.Enums.BlogPostStatus.PendingReview)
            .OrderByDescending(bp => bp.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var organizerIds = pendingEventEntities.Select(e => e.OrganizerId)
            .Concat(modifiedEventEntities.Select(e => e.OrganizerId))
            .Distinct().ToList();

        var authorIds = pendingBlogEntities.Select(bp => bp.AuthorId).Distinct().ToList();

        var userNames = new Dictionary<Guid, string>();
        foreach (var uid in organizerIds.Concat(authorIds).Distinct())
        {
            var u = await userManager.FindByIdAsync(uid.ToString());
            if (u != null) userNames[uid] = u.UserName!;
        }

        PendingEventItem Map(Domain.Entities.Event e, DateTime submittedAt) => new()
        {
            Id           = e.Id,
            Title        = e.Title,
            Slug         = e.Slug,
            CoverImageUrl = e.CoverImageUrl,
            OrganizerName = userNames.TryGetValue(e.OrganizerId, out var n) ? n : null,
            SubmittedAt  = submittedAt,
            CategoryName = e.Category?.Name,
            Status       = e.Status
        };

        var vm = new ModeratorDashboardViewModel
        {
            PendingEventsCount           = pendingEventEntities.Count,
            RecentlyModifiedEventsCount  = modifiedEventEntities.Count,
            PendingBlogPostsCount        = pendingBlogEntities.Count,
            PendingAuthorRequestsCount   = await db.OrganizerRequests.CountAsync(r => !r.IsReviewed, ct)
                                         + await db.BlogAuthorRequests.CountAsync(r => !r.IsReviewed, ct),
            PendingEvents        = pendingEventEntities.Select(e => Map(e, e.CreatedAt)).ToList(),
            RecentlyModifiedEvents = modifiedEventEntities.Select(e => Map(e, e.ModifiedAt!.Value)).ToList(),
            PendingBlogPosts     = pendingBlogEntities.Select(bp => new PendingBlogPostItem
            {
                Id           = bp.Id,
                Title        = bp.Title,
                Slug         = bp.Slug,
                CoverImageUrl = bp.CoverImageUrl,
                AuthorName   = userNames.TryGetValue(bp.AuthorId, out var an) ? an : null,
                SubmittedAt  = bp.CreatedAt
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> ApproveEvent(Guid eventId, CancellationToken ct = default)
    {
        var ev = await db.Events.FindAsync(new object[] { eventId }, ct);
        if (ev == null) return NotFound();

        ev.Status     = EventStatus.Approved;
        ev.ApprovedById = (await userManager.GetUserAsync(User))!.Id;
        ev.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["EventApproved"].Value;
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> RejectEvent(
        Guid eventId, string? reason, CancellationToken ct = default)
    {
        var ev = await db.Events.FindAsync(new object[] { eventId }, ct);
        if (ev == null) return NotFound();

        ev.Status          = EventStatus.Rejected;
        ev.RejectionReason = reason;
        await db.SaveChangesAsync(ct);

        TempData["Error"] = localizer["EventRejected"].Value;
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> ApproveBlogPost(Guid postId, CancellationToken ct = default)
    {
        var post = await db.BlogPosts.FindAsync(new object[] { postId }, ct);
        if (post == null) return NotFound();

        post.Status      = Domain.Enums.BlogPostStatus.Published;
        post.PublishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["BlogPostApproved"].Value;
        return RedirectToAction(nameof(BlogPosts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> RejectBlogPost(
        Guid postId, string? reason, CancellationToken ct = default)
    {
        var post = await db.BlogPosts.FindAsync(new object[] { postId }, ct);
        if (post == null) return NotFound();

        post.Status = Domain.Enums.BlogPostStatus.Rejected;
        await db.SaveChangesAsync(ct);

        TempData["Error"] = localizer["BlogPostRejected"].Value;
        return RedirectToAction(nameof(BlogPosts));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UserRoles(string? search, CancellationToken ct = default)
    {
        ViewData["AdminPage"] = "UserRoles";
        ViewData["AdminPageTitle"] = localizer["UserRoles"].Value;

        var vm = new UserRolesViewModel
        {
            SearchQuery    = search,
            AvailableRoles = KazanlakEvents.Domain.Enums.UserRoles.All.ToList()
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            var users = await userManager.Users
                .Where(u => u.UserName!.ToLower().Contains(lower)
                         || u.Email!.ToLower().Contains(lower))
                .Take(30)
                .ToListAsync(ct);

            foreach (var u in users)
            {
                var initials = BuildInitials(u.UserName ?? "");
                vm.Users.Add(new UserRoleItem
                {
                    Id       = u.Id,
                    UserName = u.UserName ?? "",
                    Email    = u.Email,
                    Initials = initials,
                    Roles    = (await userManager.GetRolesAsync(u)).ToList()
                });
            }
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AssignRole(Guid userId, string role, string? search)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        TempData["Success"] = localizer["RoleAdded"].Value;
        return RedirectToAction(nameof(UserRoles), new { search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RemoveRole(Guid userId, string role, string? search)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        if (await userManager.IsInRoleAsync(user, role))
            await userManager.RemoveFromRoleAsync(user, role);

        TempData["Success"] = localizer["RoleRemoved"].Value;
        return RedirectToAction(nameof(UserRoles), new { search });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Tickets(
        string? eventFilter, string? userFilter, string? statusFilter,
        int page = 1, CancellationToken ct = default)
    {
        ViewData["AdminPage"] = "Tickets";
        ViewData["AdminPageTitle"] = localizer["Tickets"].Value;

        const int pageSize = 30;

        var query = db.Tickets
            .Include(t => t.TicketType)
                .ThenInclude(tt => tt.Event)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventFilter))
        {
            if (Guid.TryParse(eventFilter, out var eid))
                query = query.Where(t => t.TicketType.EventId == eid);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter)
            && Enum.TryParse<TicketStatus>(statusFilter, out var ts))
        {
            query = query.Where(t => t.Status == ts);
        }

        var total  = await query.CountAsync(ct);
        var raw    = await query
            .OrderByDescending(t => t.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var holderIds = raw.Where(t => t.HolderId.HasValue)
            .Select(t => t.HolderId!.Value).Distinct().ToList();

        var holderNames = new Dictionary<Guid, string>();
        foreach (var uid in holderIds)
        {
            var u = await userManager.FindByIdAsync(uid.ToString());
            if (u != null) holderNames[uid] = u.UserName!;
        }

        var items = raw
            .Where(t => string.IsNullOrWhiteSpace(userFilter) ||
                        (t.HolderId.HasValue && holderNames.TryGetValue(t.HolderId.Value, out var hn)
                            && hn.Contains(userFilter, StringComparison.OrdinalIgnoreCase)) ||
                        (t.HolderEmail?.Contains(userFilter, StringComparison.OrdinalIgnoreCase) == true))
            .Select(t => new TicketAdminItem
            {
                Id             = t.Id,
                TicketNumber   = t.TicketNumber,
                EventTitle     = t.TicketType.Event.Title,
                EventId        = t.TicketType.EventId,
                HolderName     = t.HolderId.HasValue && holderNames.TryGetValue(t.HolderId.Value, out var n) ? n : null,
                HolderEmail    = t.HolderEmail,
                TicketTypeName = t.TicketType.Name,
                Status         = t.Status,
                IssuedAt       = t.IssuedAt,
                QrCode         = t.QrCode
            }).ToList();

        var eventOptions = await db.Events
            .OrderBy(e => e.Title)
            .Select(e => new EventFilterOption { Id = e.Id, Title = e.Title })
            .ToListAsync(ct);

        return View(new TicketListViewModel
        {
            Tickets      = items,
            CurrentPage  = page,
            TotalPages   = (int)Math.Ceiling(total / (double)pageSize),
            EventFilter  = eventFilter,
            UserFilter   = userFilter,
            StatusFilter = statusFilter,
            EventOptions = eventOptions
        });
    }

    // ── Events management ────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> Events(string? filter = null, int page = 1, CancellationToken ct = default)
    {
        ViewData["AdminPage"]      = "Events";
        ViewData["AdminPageTitle"] = localizer["Events"].Value;

        const int pageSize = 30;
        var cutoff = DateTime.UtcNow.AddDays(-30);

        IQueryable<Domain.Entities.Event> query = db.Events.Include(e => e.Category);

        query = filter switch
        {
            "pending"  => query.Where(e => e.Status == EventStatus.PendingApproval),
            "modified" => query.Where(e => e.ModifiedAt != null && e.ModifiedAt >= cutoff
                               && e.Status != EventStatus.PendingApproval),
            _          => query
        };

        var total = await query.CountAsync(ct);
        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var organizerIds = events.Select(e => e.OrganizerId).Distinct().ToList();
        var userNames    = new Dictionary<Guid, string>();
        foreach (var uid in organizerIds)
        {
            var u = await userManager.FindByIdAsync(uid.ToString());
            if (u != null) userNames[uid] = u.UserName!;
        }

        var items = events.Select(e => new PendingEventItem
        {
            Id            = e.Id,
            Title         = e.Title,
            Slug          = e.Slug,
            CoverImageUrl = e.CoverImageUrl,
            OrganizerName = userNames.TryGetValue(e.OrganizerId, out var n) ? n : null,
            SubmittedAt   = e.CreatedAt,
            CategoryName  = e.Category?.Name,
            Status        = e.Status
        }).ToList();

        var vm = new AdminEventsViewModel
        {
            Events       = items,
            Filter       = filter,
            CurrentPage  = page,
            TotalPages   = (int)Math.Ceiling(total / (double)pageSize),
            TotalCount   = total
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteEvent(Guid eventId, CancellationToken ct = default)
    {
        var ev = await db.Events.FindAsync(new object[] { eventId }, ct);
        if (ev == null) return NotFound();

        ev.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["EventDeleted"].Value;
        return RedirectToAction(nameof(Events));
    }

    // ── Blog management ──────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> BlogPosts(int page = 1, string? status = null, CancellationToken ct = default)
    {
        ViewData["AdminPage"]      = "BlogPosts";
        ViewData["AdminPageTitle"] = localizer["ManageBlogPosts"].Value;

        const int pageSize = 30;

        BlogPostStatus? filter = status switch
        {
            "pending"   => BlogPostStatus.PendingReview,
            "published" => BlogPostStatus.Published,
            "rejected"  => BlogPostStatus.Rejected,
            "draft"     => BlogPostStatus.Draft,
            _           => null
        };

        var (posts, total) = await blogService.GetAllAsync(page, pageSize, filter, ct);

        var authorIds   = posts.Select(p => p.AuthorId).Distinct().ToList();
        var authorNames = await db.UserProfiles
            .Where(up => authorIds.Contains(up.UserId))
            .Select(up => new { up.UserId, up.FirstName, up.LastName })
            .ToDictionaryAsync(up => up.UserId, up => $"{up.FirstName} {up.LastName}", ct);

        var vm = new KazanlakEvents.Web.ViewModels.Blog.BlogIndexViewModel
        {
            Posts        = posts.Select(p => new KazanlakEvents.Web.ViewModels.Blog.BlogPostCardViewModel
            {
                Id            = p.Id,
                Title         = p.Title,
                Slug          = p.Slug,
                Excerpt       = p.Excerpt,
                CoverImageUrl = p.CoverImageUrl,
                CategoryName  = p.Category?.Name,
                CategoryId    = p.CategoryId,
                IsFeatured    = p.IsFeatured,
                Status        = p.Status,
                PublishedAt   = p.PublishedAt,
                ViewCount     = p.ViewCount,
                AuthorName    = authorNames.TryGetValue(p.AuthorId, out var n) ? n : null
            }).ToList(),
            StatusFilter = status,
            CurrentPage  = page,
            TotalPages   = (int)Math.Ceiling(total / (double)pageSize),
            TotalPosts   = total
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> SetFeatured(Guid id, string? returnStatus = null, CancellationToken ct = default)
    {
        await blogService.SetFeaturedAsync(id, ct);
        return RedirectToAction(nameof(BlogPosts), new { status = returnStatus });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> DeleteBlogPost(Guid id, CancellationToken ct = default)
    {
        await blogService.DeleteAsync(id, ct);
        TempData["Success"] = localizer["BlogPostDeleted"].Value;
        return RedirectToAction(nameof(BlogPosts));
    }

    // ── Sponsors management ──────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Sponsors(CancellationToken ct = default)
    {
        ViewData["AdminPage"]      = "Sponsors";
        ViewData["AdminPageTitle"] = localizer["Sponsors"].Value;

        var sponsors = await sponsorService.GetAllAsync(ct);
        var vm = sponsors.Select(s => new SponsorViewModel
        {
            Id          = s.Id,
            Name        = s.Name,
            LogoUrl     = s.LogoUrl,
            WebsiteUrl  = s.WebsiteUrl,
            Description = s.Description,
            Tier        = s.Tier,
            IsActive    = s.IsActive
        }).ToList();

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult CreateSponsor()
    {
        ViewData["AdminPage"]      = "Sponsors";
        ViewData["AdminPageTitle"] = localizer["AddSponsor"].Value;

        var vm = new SponsorFormViewModel();
        PopulateSponsorTiers(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateSponsor(SponsorFormViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { PopulateSponsorTiers(model); return View(model); }

        string? logoUrl = null;
        if (model.LogoFile != null && model.LogoFile.Length > 0)
            logoUrl = await UploadSponsorLogoAsync(model.LogoFile, ct);

        var sponsor = new Sponsor
        {
            Name        = model.Name,
            LogoUrl     = logoUrl,
            WebsiteUrl  = model.WebsiteUrl,
            Description = model.Description,
            Tier        = model.Tier,
            IsActive    = model.IsActive
        };

        await sponsorService.CreateAsync(sponsor, ct);
        TempData["Success"] = localizer["SponsorCreated"].Value;
        return RedirectToAction(nameof(Sponsors));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> EditSponsor(Guid id, CancellationToken ct = default)
    {
        ViewData["AdminPage"]      = "Sponsors";
        ViewData["AdminPageTitle"] = localizer["EditSponsor"].Value;

        var sponsor = await sponsorService.GetByIdAsync(id, ct);
        if (sponsor == null) return NotFound();

        var vm = new SponsorFormViewModel
        {
            Id              = sponsor.Id,
            Name            = sponsor.Name,
            WebsiteUrl      = sponsor.WebsiteUrl,
            Description     = sponsor.Description,
            Tier            = sponsor.Tier,
            IsActive        = sponsor.IsActive,
            ExistingLogoUrl = sponsor.LogoUrl
        };
        PopulateSponsorTiers(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> EditSponsor(SponsorFormViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { PopulateSponsorTiers(model); return View(model); }

        var existing = await sponsorService.GetByIdAsync(model.Id!.Value, ct);
        if (existing == null) return NotFound();

        string? logoUrl = existing.LogoUrl;
        if (model.LogoFile != null && model.LogoFile.Length > 0)
        {
            if (!string.IsNullOrEmpty(existing.LogoUrl))
                await fileStorage.DeleteAsync(existing.LogoUrl, ct);
            logoUrl = await UploadSponsorLogoAsync(model.LogoFile, ct);
        }

        existing.Name        = model.Name;
        existing.LogoUrl     = logoUrl;
        existing.WebsiteUrl  = model.WebsiteUrl;
        existing.Description = model.Description;
        existing.Tier        = model.Tier;
        existing.IsActive    = model.IsActive;

        await sponsorService.UpdateAsync(existing, ct);
        TempData["Success"] = localizer["SponsorUpdated"].Value;
        return RedirectToAction(nameof(Sponsors));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteSponsor(Guid id, CancellationToken ct = default)
    {
        var sponsor = await sponsorService.GetByIdAsync(id, ct);
        if (sponsor != null && !string.IsNullOrEmpty(sponsor.LogoUrl))
            await fileStorage.DeleteAsync(sponsor.LogoUrl, ct);

        await sponsorService.DeleteAsync(id, ct);
        TempData["Success"] = localizer["SponsorDeleted"].Value;
        return RedirectToAction(nameof(Sponsors));
    }

    // ── Organized events ─────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> OrganizedEvents(CancellationToken ct = default)
    {
        ViewData["AdminPage"]      = "OrganizedEvents";
        ViewData["AdminPageTitle"] = localizer["OrganizedEvents"].Value;

        var events = await db.OrganizedEvents
            .Include(e => e.TeamMembers)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        return View(events);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> EditOrganizedEvent(Guid id, CancellationToken ct = default)
    {
        var oe = await db.OrganizedEvents
            .Include(e => e.TeamMembers.OrderBy(tm => tm.SortOrder))
            .Include(e => e.Sponsors)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (oe == null) return NotFound();

        var vm = new KazanlakEvents.Web.ViewModels.About.OrganizedEventFormViewModel
        {
            Id             = oe.Id,
            Title          = oe.Title,
            Description    = oe.Description,
            CoverImageUrl  = oe.CoverImageUrl,
            EventDate      = oe.EventDate,
            AttendeesCount = oe.AttendeesCount,
            IsActive       = oe.IsActive
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOrganizedEvent(
        KazanlakEvents.Web.ViewModels.About.OrganizedEventFormViewModel model,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return View(model);

        var oe = await db.OrganizedEvents.FirstOrDefaultAsync(e => e.Id == model.Id, ct);
        if (oe == null) return NotFound();

        oe.Title          = model.Title;
        oe.Description    = model.Description;
        oe.CoverImageUrl  = model.CoverImageUrl;
        oe.EventDate      = model.EventDate;
        oe.AttendeesCount = model.AttendeesCount;
        oe.IsActive       = model.IsActive;

        await db.SaveChangesAsync(ct);
        TempData["Success"] = localizer["OrganizedEventUpdated"].Value;
        return RedirectToAction(nameof(OrganizedEvents));
    }

    private static void PopulateSponsorTiers(SponsorFormViewModel vm)
    {
        vm.Tiers = Enum.GetValues<SponsorTier>()
            .OrderByDescending(t => t)
            .Select(t => new SelectListItem
            {
                Value    = ((int)t).ToString(),
                Text     = t.ToString(),
                Selected = t == vm.Tier
            }).ToList();
    }

    private async Task<string> UploadSponsorLogoAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        return await fileStorage.UploadAsync(stream, file.FileName, file.ContentType, ct);
    }

    private static string BuildInitials(string userName)
    {
        var parts = userName.Split(new[] { ' ', '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0
            ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}"
            : userName.Length > 0 ? char.ToUpper(userName[0]).ToString() : "?";
    }
}
