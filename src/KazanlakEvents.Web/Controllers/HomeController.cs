using AutoMapper;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Web.ViewModels.About;
using KazanlakEvents.Web.ViewModels.Event;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace KazanlakEvents.Web.Controllers;

public class HomeController(
    IEventService eventService,
    IMapper mapper,
    IApplicationDbContext context,
    ISponsorService sponsorService) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["IsHomePage"] = true;

        var nextEvent = await eventService.GetUpcomingEventsAsync(1);
        ViewBag.NextEvent = nextEvent.FirstOrDefault();

        var upcomingEvents = await eventService.GetUpcomingEventsAsync(6);
        var upcomingCards = mapper.Map<List<EventCardViewModel>>(upcomingEvents);

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                var favIds = await context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.EventId)
                    .ToListAsync();
                foreach (var card in upcomingCards)
                    card.IsFavorited = favIds.Contains(card.Id);
            }
        }

        ViewBag.UpcomingEvents = upcomingCards;

        var categories = await context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
        ViewBag.Categories = mapper.Map<List<CategoryViewModel>>(categories);

        ViewBag.TotalEvents = await context.Events
            .CountAsync(e => e.Status == EventStatus.Published || e.Status == EventStatus.Completed);
        ViewBag.TotalAttendees = await context.EventAttendances
            .Select(a => a.UserId).Distinct().CountAsync();
        ViewBag.TotalVolunteers = await context.VolunteerSignups
            .Select(v => v.UserId).Distinct().CountAsync();
        ViewBag.TotalOrganizers = await context.Events
            .Select(e => e.OrganizerId).Distinct().CountAsync();

        var allSponsors = await sponsorService.GetActiveSponsorsAsync();
        ViewBag.Sponsors = allSponsors
            .Where(s => s.Tier == Domain.Enums.SponsorTier.Gold || s.Tier == Domain.Enums.SponsorTier.Silver)
            .OrderByDescending(s => s.Tier)
            .Select(s => new Web.ViewModels.Sponsor.SponsorViewModel
            {
                Id      = s.Id,
                Name    = s.Name,
                LogoUrl = s.LogoUrl,
                Tier    = s.Tier
            }).ToList();

        var next = nextEvent.FirstOrDefault();
        if (next != null)
        {
            ViewData["AnnouncementEvent"] = next.Title;
            ViewData["AnnouncementLink"]  = Url.Action("Details", "Event", new { slug = next.Slug });
        }

        return View();
    }

    public async Task<IActionResult> About()
    {
        var vm = new AboutViewModel
        {
            TotalEventsCount    = await context.OrganizedEvents.CountAsync(e => e.IsActive),
            TotalAttendeesCount = await context.OrganizedEvents.Where(e => e.IsActive && e.AttendeesCount.HasValue).SumAsync(e => e.AttendeesCount!.Value),
            OrganizedEvents     = await context.OrganizedEvents
                .Where(e => e.IsActive)
                .OrderByDescending(e => e.EventDate)
                .Select(e => new OrganizedEventCardViewModel
                {
                    Id               = e.Id,
                    Title            = e.Title,
                    Slug             = e.Slug,
                    CoverImageUrl    = e.CoverImageUrl,
                    EventDate        = e.EventDate,
                    AttendeesCount   = e.AttendeesCount,
                    DescriptionExcerpt = e.Description.Length > 200
                        ? e.Description.Substring(0, 200)
                        : e.Description
                })
                .ToListAsync()
        };

        foreach (var card in vm.OrganizedEvents)
            card.DescriptionExcerpt = Regex.Replace(card.DescriptionExcerpt, "<[^>]+>", "");

        var organizedEventsWithTeam = await context.OrganizedEvents
            .Where(e => e.IsActive)
            .Include(e => e.TeamMembers)
            .ToListAsync();
        var rawMembers = organizedEventsWithTeam
            .SelectMany(e => e.TeamMembers)
            .Where(t => t.Role == "Organizer" || t.Role == "Coordinator")
            .GroupBy(t => t.FullName)
            .Select(g => g.OrderBy(t => t.SortOrder).First())
            .OrderBy(t => t.SortOrder)
            .ToList();

        var linkedIds = rawMembers
            .Where(t => string.IsNullOrEmpty(t.PhotoUrl) && t.LinkedUserId.HasValue)
            .Select(t => t.LinkedUserId!.Value)
            .Distinct()
            .ToList();
        var avatarMap = linkedIds.Count > 0
            ? await context.UserProfiles
                .Where(p => linkedIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.AvatarUrl)
            : new Dictionary<Guid, string?>();

        vm.TeamMembers = rawMembers
            .Select(t => new TeamMemberViewModel
            {
                Id           = t.Id,
                FullName     = t.FullName,
                PhotoUrl     = t.PhotoUrl ?? (t.LinkedUserId.HasValue && avatarMap.TryGetValue(t.LinkedUserId.Value, out var av) ? av : null),
                Description  = t.Description,
                Role         = t.Role,
                LinkedUserId = t.LinkedUserId,
                SortOrder    = t.SortOrder
            })
            .ToList();

        return View(vm);
    }

    public async Task<IActionResult> OrganizedEvent(string slug)
    {
        var ev = await context.OrganizedEvents
            .Include(e => e.TeamMembers)
            .Include(e => e.Sponsors)
            .FirstOrDefaultAsync(e => e.Slug == slug && e.IsActive);

        if (ev == null) return NotFound();

        var linkedIds = ev.TeamMembers
            .Where(t => string.IsNullOrEmpty(t.PhotoUrl) && t.LinkedUserId.HasValue)
            .Select(t => t.LinkedUserId!.Value)
            .Distinct()
            .ToList();
        var avatarMap = linkedIds.Count > 0
            ? await context.UserProfiles
                .Where(p => linkedIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.AvatarUrl)
            : new Dictionary<Guid, string?>();

        static int RoleOrder(string role) => role switch
        {
            "Organizer"          => 0,
            "Coordinator"        => 1,
            "Volunteer"          => 2,
            "Technical Support"  => 3,
            _                    => 4
        };

        var groups = ev.TeamMembers
            .OrderBy(t => RoleOrder(t.Role)).ThenBy(t => t.SortOrder)
            .GroupBy(t => t.Role)
            .Select(g => new TeamRoleGroupViewModel
            {
                Role    = g.Key,
                Members = g.Select(t => new TeamMemberViewModel
                {
                    Id            = t.Id,
                    FullName      = t.FullName,
                    PhotoUrl      = t.PhotoUrl ?? (t.LinkedUserId.HasValue && avatarMap.TryGetValue(t.LinkedUserId.Value, out var av) ? av : null),
                    Description   = t.Description,
                    Quote         = t.Quote,
                    Role          = t.Role,
                    TagList       = string.IsNullOrEmpty(t.Tags) ? [] : t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    InstagramUrl  = t.InstagramUrl,
                    EmailAddress  = t.EmailAddress,
                    LinkedUserId  = t.LinkedUserId,
                    SortOrder     = t.SortOrder
                }).ToList()
            })
            .ToList();

        var vm = new OrganizedEventDetailViewModel
        {
            Id             = ev.Id,
            Title          = ev.Title,
            Slug           = ev.Slug,
            Description    = ev.Description,
            CoverImageUrl  = ev.CoverImageUrl,
            EventDate      = ev.EventDate,
            AttendeesCount = ev.AttendeesCount,
            TeamGroups     = groups,
            Sponsors       = ev.Sponsors.Select(s => new OrganizedEventSponsorViewModel
            {
                Id          = s.Id,
                CompanyName = s.CompanyName,
                LogoUrl     = s.LogoUrl,
                Description = s.Description,
                WebsiteUrl  = s.WebsiteUrl
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Contact() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(string name, string email, string message)
    {
        TempData["Success"] = "ContactThankYou";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult Privacy() => View();

    public IActionResult Terms() => View();

    public IActionResult ApiDocs() => View();

    public IActionResult Error(string? message = null)
    {
        ViewBag.ErrorMessage = message ?? "An unexpected error occurred.";
        return View();
    }

    [Route("Home/StatusCode/{code:int}")]
    public new IActionResult StatusCode(int code) => code switch
    {
        404 => View("NotFound"),
        500 => View("ServerError"),
        _   => View("Error")
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, Path = "/" });

        return LocalRedirect(returnUrl ?? "/");
    }
}
