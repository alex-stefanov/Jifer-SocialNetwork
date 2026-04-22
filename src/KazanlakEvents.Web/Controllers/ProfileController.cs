using AutoMapper;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.Resources;
using KazanlakEvents.Web.ViewModels.Event;
using KazanlakEvents.Web.ViewModels.Profile;
using KazanlakEvents.Web.ViewModels.Volunteer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace KazanlakEvents.Web.Controllers;

public class ProfileController(
    IUserService userService,
    IEventService eventService,
    ITicketService ticketService,
    IVolunteerService volunteerService,
    INotificationService notificationService,
    IMapper mapper,
    ICurrentUserService currentUser,
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IFileStorageService fileStorage,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? id = null, string? username = null, string tab = "events", CancellationToken ct = default)
    {
        if (id == null && username == null && !currentUser.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var (profileUserId, displayUserName) = await ResolveUserAsync(id, username);
        if (profileUserId == Guid.Empty) return NotFound();

        var profile = await userService.GetProfileAsync(profileUserId, ct);
        if (profile == null) return NotFound();

        var isOwnProfile = currentUser.IsAuthenticated && currentUser.UserId == profileUserId;

        // Admin-tier profiles are private — only visible to the account owner
        if (!isOwnProfile)
        {
            var viewedUser = await userManager.FindByIdAsync(profileUserId.ToString());
            if (viewedUser != null)
            {
                var viewedRoles = await userManager.GetRolesAsync(viewedUser);
                if (viewedRoles.Any(r => r is "Admin" or "SuperAdmin" or "Moderator"))
                    return NotFound();
            }
        }

        if (tab == "tickets" && !isOwnProfile)
            tab = "events";

        // Always load stats — sequential to avoid concurrent DbContext access
        var organizedCount = await userService.GetOrganizedEventCountAsync(profileUserId, ct);
        var attendedCount  = await userService.GetAttendedEventCountAsync(profileUserId, ct);
        var followerCount  = await userService.GetFollowerCountAsync(profileUserId, ct);
        var followingCount = await userService.GetFollowingCountAsync(profileUserId, ct);
        var volunteerStats = await volunteerService.GetVolunteerStatsAsync(profileUserId, ct);

        var isFollowing = false;
        if (currentUser.IsAuthenticated && !isOwnProfile)
            isFollowing = await userService.IsFollowingAsync(currentUser.UserId!.Value, profileUserId, ct);

        var appUser = await userManager.FindByIdAsync(profileUserId.ToString());
        var vm = new UserProfileViewModel
        {
            UserId            = profileUserId,
            UserName          = displayUserName,
            FullName          = profile.FullName,
            Bio               = profile.Bio,
            AvatarUrl         = profile.AvatarUrl,
            City              = profile.City,
            PhoneNumber       = profile.PhoneNumber,
            PreferredLanguage = profile.PreferredLanguage,
            MemberSince       = appUser?.CreatedAt ?? profile.CreatedAt,
            EventsOrganized   = organizedCount,
            EventsAttended    = attendedCount,
            FollowerCount     = followerCount,
            FollowingCount    = followingCount,
            VolunteerHours    = volunteerStats.TotalHours,
            IsOwnProfile      = isOwnProfile,
            IsFollowing       = isFollowing
        };

        var profileRoles = appUser != null
            ? (IList<string>)await userManager.GetRolesAsync(appUser)
            : Array.Empty<string>();
        ViewBag.IsOrganizerProfile = profileRoles.Contains("Organizer");

        ViewBag.ActiveTab = tab;

        switch (tab)
        {
            case "favorites":
                var favEvents = await userService.GetFavoriteEventsWithDetailsAsync(profileUserId, ct);
                vm.FavoriteEvents = mapper.Map<List<EventCardViewModel>>(favEvents);
                foreach (var card in vm.FavoriteEvents) card.IsFavorited = true;
                break;

            case "tickets" when isOwnProfile:
                var tickets = await ticketService.GetUserTicketsAsync(profileUserId, ct);
                var now = DateTime.UtcNow;
                vm.UpcomingTickets = tickets
                    .Where(t => t.TicketType?.Event?.StartDate > now &&
                                t.Status != TicketStatus.Cancelled &&
                                t.Status != TicketStatus.Transferred)
                    .Select(t => new TicketViewModel
                    {
                        Id                 = t.Id,
                        TicketNumber       = t.TicketNumber,
                        QrCode             = t.QrCode,
                        Status             = t.Status,
                        IssuedAt           = t.IssuedAt,
                        CheckedInAt        = t.CheckedInAt,
                        EventTitle         = t.TicketType?.Event?.Title ?? string.Empty,
                        EventSlug          = t.TicketType?.Event?.Slug ?? string.Empty,
                        EventCoverImageUrl = t.TicketType?.Event?.CoverImageUrl,
                        EventStartDate     = t.TicketType?.Event?.StartDate ?? DateTime.MinValue,
                        TicketTypeName     = t.TicketType?.Name ?? string.Empty,
                        QrCodeImageUrl     = t.QrCodeImageUrl
                    }).ToList();
                vm.PastTickets = tickets
                    .Where(t => t.TicketType?.Event?.StartDate <= now ||
                                t.Status == TicketStatus.Cancelled ||
                                t.Status == TicketStatus.Transferred)
                    .Select(t => new TicketViewModel
                    {
                        Id                 = t.Id,
                        TicketNumber       = t.TicketNumber,
                        QrCode             = t.QrCode,
                        Status             = t.Status,
                        IssuedAt           = t.IssuedAt,
                        CheckedInAt        = t.CheckedInAt,
                        EventTitle         = t.TicketType?.Event?.Title ?? string.Empty,
                        EventSlug          = t.TicketType?.Event?.Slug ?? string.Empty,
                        EventCoverImageUrl = t.TicketType?.Event?.CoverImageUrl,
                        EventStartDate     = t.TicketType?.Event?.StartDate ?? DateTime.MinValue,
                        TicketTypeName     = t.TicketType?.Name ?? string.Empty,
                        QrCodeImageUrl     = t.QrCodeImageUrl
                    }).ToList();
                break;

            case "volunteer":
                var signups = await volunteerService.GetUserSignupsAsync(profileUserId, ct);
                vm.VolunteerSignups = mapper.Map<List<VolunteerSignupViewModel>>(signups);
                break;

            default: // "events" and "about"
                var organized = await eventService.GetOrganizerEventsAsync(profileUserId, 50, ct);
                vm.OrganizedEvents = mapper.Map<List<EventCardViewModel>>(organized);
                break;
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(Guid userId, CancellationToken ct = default)
    {
        await userService.FollowAsync(currentUser.UserId!.Value, userId, ct);
        TempData["Success"] = localizer["Followed"].Value;
        return RedirectToAction(nameof(Index), new { id = userId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow(Guid userId, CancellationToken ct = default)
    {
        await userService.UnfollowAsync(currentUser.UserId!.Value, userId, ct);
        TempData["Success"] = localizer["Unfollowed"].Value;
        return RedirectToAction(nameof(Index), new { id = userId });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(CancellationToken ct = default)
    {
        var profile = await userService.GetProfileAsync(currentUser.UserId!.Value, ct);
        if (profile == null) return NotFound();

        return View(new EditProfileViewModel
        {
            FirstName         = profile.FirstName,
            LastName          = profile.LastName,
            Bio               = profile.Bio,
            City              = profile.City,
            PhoneNumber       = profile.PhoneNumber,
            DateOfBirth       = profile.DateOfBirth,
            PreferredLanguage = profile.PreferredLanguage,
            ExistingAvatarUrl = profile.AvatarUrl
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return View(model);

        var profile = await userService.GetProfileAsync(currentUser.UserId!.Value, ct);
        if (profile == null) return NotFound();

        if (model.Avatar != null)
        {
            await using var stream = model.Avatar.OpenReadStream();
            profile.AvatarUrl = await fileStorage.UploadAsync(
                stream, model.Avatar.FileName, model.Avatar.ContentType, ct);
        }
        else
        {
            profile.AvatarUrl = model.ExistingAvatarUrl;
        }

        profile.FirstName         = model.FirstName;
        profile.LastName          = model.LastName;
        profile.Bio               = model.Bio;
        profile.City              = model.City;
        profile.PhoneNumber       = model.PhoneNumber;
        profile.DateOfBirth       = model.DateOfBirth;
        profile.PreferredLanguage = model.PreferredLanguage;

        await userService.UpdateProfileAsync(profile, ct);
        TempData["Success"] = localizer["ProfileUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferTicket(
        Guid ticketId, string recipientEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            TempData["Error"] = localizer["RecipientEmail"].Value + " is required.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        try
        {
            var recipient = await userManager.FindByEmailAsync(recipientEmail);

            Ticket ticket;
            if (recipient != null)
            {
                ticket = await ticketService.TransferTicketAsync(ticketId, recipient.Id, ct);

                var senderProfile = await userService.GetProfileAsync(currentUser.UserId!.Value, ct);
                var senderName    = senderProfile?.FullName ?? currentUser.UserName ?? "Someone";

                await notificationService.SendNotificationAsync(
                    recipient.Id,
                    NotificationType.TicketTransferred,
                    "You received a ticket!",
                    $"{senderName} transferred ticket {ticket.TicketNumber} to you.",
                    ct: ct);
            }
            else
            {
                ticket = await ticketService.TransferTicketToEmailAsync(ticketId, recipientEmail, ct);
            }

            await notificationService.SendNotificationAsync(
                currentUser.UserId!.Value,
                NotificationType.TicketTransferred,
                localizer["TicketTransferSuccess"].Value,
                $"You transferred ticket {ticket.TicketNumber} to {recipientEmail}.",
                ct: ct);

            TempData["Success"] = localizer["TicketTransferSuccess"].Value;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "tickets" });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> RequestOrganizer(CancellationToken ct = default)
    {
        var userId = currentUser.UserId!.Value;

        if (User.IsInRole("Organizer") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("Moderator"))
        {
            TempData["Success"] = localizer["OrganizerRequestAlreadyPending"].Value;
            return RedirectToAction(nameof(Index));
        }

        var existing = await db.OrganizerRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && !r.IsReviewed, ct);
        if (existing != null)
        {
            TempData["Error"] = localizer["OrganizerRequestAlreadyPending"].Value;
            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestOrganizer(string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            ModelState.AddModelError(nameof(reason), "Reason is required.");
            return View();
        }

        var userId = currentUser.UserId!.Value;

        var existing = await db.OrganizerRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && !r.IsReviewed, ct);
        if (existing != null)
        {
            TempData["Error"] = localizer["OrganizerRequestAlreadyPending"].Value;
            return RedirectToAction(nameof(Index));
        }

        db.OrganizerRequests.Add(new OrganizerRequest
        {
            UserId = userId,
            Reason = reason.Trim()
        });
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["OrganizerRequestSubmitted"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFollowers(Guid userId, CancellationToken ct = default)
    {
        var followerIds = await db.Follows
            .Where(f => f.FolloweeId == userId)
            .Select(f => f.FollowerId)
            .ToListAsync(ct);

        var profiles = await db.UserProfiles
            .Where(p => followerIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.FirstName, p.LastName, p.AvatarUrl })
            .ToListAsync(ct);

        var users = await userManager.Users
            .Where(u => followerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName })
            .ToListAsync();

        var result = followerIds.Select(id =>
        {
            var profile = profiles.FirstOrDefault(p => p.UserId == id);
            var user    = users.FirstOrDefault(u => u.Id == id);
            var fullName = profile != null
                ? $"{profile.FirstName} {profile.LastName}".Trim()
                : user?.UserName ?? string.Empty;
            return new
            {
                Id        = id,
                UserName  = user?.UserName ?? string.Empty,
                FullName  = fullName,
                AvatarUrl = profile?.AvatarUrl
            };
        });

        return Json(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFollowing(Guid userId, CancellationToken ct = default)
    {
        var followeeIds = await db.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FolloweeId)
            .ToListAsync(ct);

        var profiles = await db.UserProfiles
            .Where(p => followeeIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.FirstName, p.LastName, p.AvatarUrl })
            .ToListAsync(ct);

        var users = await userManager.Users
            .Where(u => followeeIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName })
            .ToListAsync();

        var result = followeeIds.Select(id =>
        {
            var profile = profiles.FirstOrDefault(p => p.UserId == id);
            var user    = users.FirstOrDefault(u => u.Id == id);
            var fullName = profile != null
                ? $"{profile.FirstName} {profile.LastName}".Trim()
                : user?.UserName ?? string.Empty;
            return new
            {
                Id        = id,
                UserName  = user?.UserName ?? string.Empty,
                FullName  = fullName,
                AvatarUrl = profile?.AvatarUrl
            };
        });

        return Json(result);
    }

    private async Task<(Guid UserId, string UserName)> ResolveUserAsync(Guid? id, string? username)
    {
        if (username != null)
        {
            var u = await userManager.FindByNameAsync(username);
            return u != null ? (u.Id, u.UserName!) : (Guid.Empty, string.Empty);
        }
        if (id.HasValue)
        {
            var u = await userManager.FindByIdAsync(id.Value.ToString());
            return u != null ? (u.Id, u.UserName!) : (Guid.Empty, string.Empty);
        }
        if (currentUser.IsAuthenticated)
            return (currentUser.UserId!.Value, currentUser.UserName ?? string.Empty);

        return (Guid.Empty, string.Empty);
    }
}
