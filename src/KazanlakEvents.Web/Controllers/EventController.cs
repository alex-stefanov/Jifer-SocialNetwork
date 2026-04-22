using AutoMapper;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Web.Extensions;
using KazanlakEvents.Web.Resources;
using KazanlakEvents.Web.ViewModels.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace KazanlakEvents.Web.Controllers;

public class EventController(
    IEventService eventService,
    IRatingService ratingService,
    ITicketService ticketService,
    ISponsorService sponsorService,
    IMapper mapper,
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IFileStorageService fileStorage,
    IStringLocalizer<SharedResource> localizer,
    ILogger<EventController> logger) : Controller
{
    private const int PageSize = 12;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1, int? categoryId = null, string? search = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        string? tag = null, bool? hasVolunteerTasks = null, CancellationToken ct = default)
    {
        var (items, total) = await eventService.GetEventsPagedAsync(
            page, PageSize, categoryId: categoryId,
            fromDate: fromDate, toDate: toDate,
            searchQuery: search, tag: tag, hasVolunteerTasks: hasVolunteerTasks, ct: ct);

        var categoryEntities = await db.Categories
            .Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(ct);
        var categories = mapper.Map<List<CategoryViewModel>>(categoryEntities);

        var upcomingQuery = db.Events
            .Where(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow);
        if (categoryId.HasValue)
            upcomingQuery = upcomingQuery.Where(e => e.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(search))
            upcomingQuery = upcomingQuery.Where(e =>
                e.Title.Contains(search) ||
                (e.ShortDescription != null && e.ShortDescription.Contains(search)));
        if (!string.IsNullOrWhiteSpace(tag))
            upcomingQuery = upcomingQuery.Where(e => e.EventTags.Any(et => et.Tag.Name == tag));
        if (hasVolunteerTasks == true)
            upcomingQuery = upcomingQuery.Where(e => e.VolunteerTasks.Any());
        var upcomingCount = await upcomingQuery.CountAsync(ct);

        var cards = mapper.Map<List<EventCardViewModel>>(items);

        if (currentUser.IsAuthenticated)
        {
            var uid = currentUser.UserId!.Value;
            var favIds = await db.Favorites
                .Where(f => f.UserId == uid)
                .Select(f => f.EventId)
                .ToListAsync(ct);
            foreach (var card in cards)
                card.IsFavorited = favIds.Contains(card.Id);
        }

        var vm = new EventListViewModel
        {
            Events        = cards,
            CurrentPage   = page,
            TotalPages    = (int)Math.Ceiling(total / (double)PageSize),
            TotalCount    = total,
            UpcomingCount = upcomingCount,
            CategoryId    = categoryId,
            SearchQuery   = search,
            FromDate      = fromDate,
            ToDate        = toDate,
            Tag                = tag,
            HasVolunteerTasks  = hasVolunteerTasks,
            Categories         = categories
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string slug, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventBySlugAsync(slug, ct);
        if (ev == null) return NotFound();

        await eventService.IncrementViewCountAsync(ev.Id, ct);
        var vm = mapper.Map<EventDetailViewModel>(ev);

        if (currentUser.IsAuthenticated)
        {
            var uid = currentUser.UserId!.Value;
            vm.IsOrganizer = ev.OrganizerId == uid;
            vm.IsFavorited = await db.Favorites.AnyAsync(f => f.EventId == ev.Id && f.UserId == uid, ct);
            var attendance = await db.EventAttendances
                .FirstOrDefaultAsync(a => a.EventId == ev.Id && a.UserId == uid, ct);
            vm.UserAttendance = attendance?.Status;
            var rating = await db.Ratings
                .FirstOrDefaultAsync(r => r.EventId == ev.Id && r.UserId == uid, ct);
            vm.CurrentUserRating = rating?.Score;
        }

        var hasTickets = await db.TicketTypes.AnyAsync(t => t.EventId == ev.Id, ct);
        if (!hasTickets)
        {
            db.TicketTypes.Add(new TicketType
            {
                EventId      = ev.Id,
                Name         = "Free Admission",
                Price        = 0,
                Quantity     = ev.Capacity ?? 0,
                Currency     = "EUR",
                SortOrder    = 0,
                MaxPerOrder  = 10
            });
            await db.SaveChangesAsync(ct);
            ev = await eventService.GetEventBySlugAsync(slug, ct) ?? ev;
            vm = mapper.Map<EventDetailViewModel>(ev);
            if (currentUser.IsAuthenticated)
            {
                var uid2 = currentUser.UserId!.Value;
                vm.IsOrganizer    = ev.OrganizerId == uid2;
                vm.IsFavorited    = await db.Favorites.AnyAsync(f => f.EventId == ev.Id && f.UserId == uid2, ct);
                var att2 = await db.EventAttendances.FirstOrDefaultAsync(a => a.EventId == ev.Id && a.UserId == uid2, ct);
                vm.UserAttendance = att2?.Status;
                var rat2 = await db.Ratings.FirstOrDefaultAsync(r => r.EventId == ev.Id && r.UserId == uid2, ct);
                vm.CurrentUserRating = rat2?.Score;
            }
        }

        var ticketTypeEntities = await db.TicketTypes
            .Where(t => t.EventId == ev.Id)
            .OrderBy(t => t.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);
        vm.TicketTypes = mapper.Map<IReadOnlyList<TicketTypeViewModel>>(ticketTypeEntities);

        vm.HasVolunteerTasks = await db.VolunteerTasks.AnyAsync(t => t.EventId == ev.Id, ct);

        if (currentUser.IsAuthenticated)
        {
            var uid3 = currentUser.UserId!.Value;
            vm.UserCurrentTicketCount = await db.Tickets
                .Where(t => t.HolderId == uid3 &&
                            t.TicketType.EventId == ev.Id &&
                            t.Status != Domain.Enums.TicketStatus.Cancelled)
                .CountAsync(ct);
        }

        if (ev.SeriesId.HasValue)
        {
            var series = await eventService.GetSeriesAsync(ev.SeriesId.Value, ct);
            vm.SeriesTitle = series?.Title;
        }

        var eventSponsors = await sponsorService.GetEventSponsorsAsync(ev.Id, ct);
        vm.Sponsors = eventSponsors.Select(es => new ViewModels.Sponsor.SponsorViewModel
        {
            Id              = es.Sponsor.Id,
            Name            = es.Sponsor.Name,
            LogoUrl         = es.Sponsor.LogoUrl,
            WebsiteUrl      = es.Sponsor.WebsiteUrl,
            Description     = es.Sponsor.Description,
            Tier            = es.Sponsor.Tier,
            IsActive        = es.Sponsor.IsActive,
            ImpressionCount = es.ImpressionCount,
            ClickCount      = es.ClickCount
        }).ToList();

        var allComments = await db.Comments
            .Where(c => c.EventId == ev.Id && !c.IsHidden)
            .OrderBy(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        if (allComments.Count > 0)
        {
            var authorIds = allComments.Select(c => c.UserId).Distinct().ToList();
            var profiles = await db.UserProfiles
                .AsNoTracking()
                .Where(p => authorIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p, ct);

            var commentVms = allComments.Select(c =>
            {
                profiles.TryGetValue(c.UserId, out var prof);
                return new CommentViewModel
                {
                    Id              = c.Id,
                    Content         = c.Content,
                    AuthorName      = prof?.FullName ?? "—",
                    AuthorAvatarUrl = prof?.AvatarUrl,
                    AuthorId        = c.UserId,
                    CreatedAt       = c.CreatedAt,
                    IsEdited        = c.IsEdited,
                    UpvoteCount     = c.UpvoteCount,
                    ParentCommentId = c.ParentCommentId,
                    Replies         = new List<CommentViewModel>()
                };
            }).ToList();

            var lookup = commentVms.ToDictionary(c => c.Id);
            var rootComments = new List<CommentViewModel>();
            foreach (var comment in commentVms)
            {
                if (comment.ParentCommentId.HasValue && lookup.TryGetValue(comment.ParentCommentId.Value, out var parent))
                    parent.Replies.Add(comment);
                else
                    rootComments.Add(comment);
            }

            vm.Comments = rootComments;
        }

        var orgProfile = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == ev.OrganizerId, ct);
        vm.OrganizerName = orgProfile?.FullName.Trim() is { Length: > 0 } n ? n : "—";
        vm.OrganizerAvatarUrl = orgProfile?.AvatarUrl;

        ViewData["OgType"]        = "event";
        ViewData["OgTitle"]       = ev.Title;
        ViewData["OgDescription"] = ev.ShortDescription
            ?? (ev.Description?.Length > 0
                ? ev.Description[..Math.Min(160, ev.Description.Length)]
                : null);
        ViewData["OgImage"]       = ev.CoverImageUrl;

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    public async Task<IActionResult> MyEvents(CancellationToken ct = default)
    {
        var uid = currentUser.UserId!.Value;
        var events = await eventService.GetByOrganizerAsync(uid, ct);

        var viewModels = events.Select(e => new MyEventViewModel
        {
            Id             = e.Id,
            Title          = e.Title,
            Slug           = e.Slug,
            StartDate      = e.StartDate,
            Status         = e.Status,
            CoverImageUrl  = e.CoverImageUrl,
            OrganizerId    = e.OrganizerId,
            TicketsSold    = e.TicketTypes.Sum(tt => tt.QuantitySold),
            AttendeeCount  = e.Attendances.Count(a => a.Status == AttendanceStatus.Going),
            HasVolunteerTasks = e.VolunteerTasks.Any(),
            VolunteerCount = e.VolunteerTasks
                              .SelectMany(t => t.Shifts)
                              .SelectMany(s => s.Signups)
                              .Count()
        }).ToList();

        return View(viewModels);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!User.IsInRole("Organizer") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            return View("CreateNotOrganizer");
        }

        var vm = new EventCreateViewModel();
        await PopulateDropdowns(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventCreateViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => $"{x.Key}: {string.Join("; ", x.Value!.Errors.Select(e => e.ErrorMessage))}");
            logger.LogWarning("Event Create ModelState invalid. Errors: {Errors}", string.Join(" | ", errors));
            await PopulateDropdowns(model, ct);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.NewVenueName))
        {
            db.VenueRequests.Add(new VenueRequest
            {
                RequestedByUserId = currentUser.UserId!.Value,
                Name              = model.NewVenueName.Trim(),
                Address           = model.NewVenueAddress?.Trim() ?? model.NewVenueName.Trim(),
                Latitude          = model.NewVenueLat,
                Longitude         = model.NewVenueLng
            });
            await db.SaveChangesAsync(ct);
            model.VenueId = null; // event will have no venue until request is approved
        }

        var ev = mapper.Map<Event>(model);
        if (model.CoverImage != null)
        {
            await using var stream = model.CoverImage.OpenReadStream();
            ev.CoverImageUrl = await fileStorage.UploadAsync(
                stream, model.CoverImage.FileName, model.CoverImage.ContentType, ct);
        }

        if (model.IsRecurring)
        {
            var rrule = BuildRrule(model);
            await eventService.CreateRecurringEventAsync(ev, rrule, model.RruleCount, ct);
            TempData["Success"] = localizer["RecurringEventCreated"].Value;
            return RedirectToAction(nameof(Index));
        }

        var created = await eventService.CreateEventAsync(ev, model.SelectedTagIds, ct);

        var namedInputs = model.TicketTypeInputs.Where(t => !string.IsNullOrEmpty(t.Name)).ToList();
        if (namedInputs.Any())
        {
            if (created.Capacity.HasValue && created.Capacity > 0)
            {
                var totalQty = namedInputs.Sum(t => t.Quantity > 0 ? t.Quantity : 0);
                if (totalQty != created.Capacity.Value)
                {
                    await eventService.DeleteEventAsync(created.Id, ct);
                    ModelState.AddModelError(string.Empty,
                        localizer["TicketQuantityMustMatchCapacity",
                            totalQty, created.Capacity.Value].Value);
                    await PopulateDropdowns(model, ct);
                    return View(model);
                }
            }

            int sort = 0;
            foreach (var tt in namedInputs)
                db.TicketTypes.Add(new TicketType
                {
                    EventId = created.Id, Name = tt.Name, Price = 0,
                    Quantity = tt.Quantity > 0 ? tt.Quantity : 0,
                    Description = tt.Description, Currency = "EUR",
                    SortOrder = sort++, MaxPerOrder = 10
                });
            await db.SaveChangesAsync(ct);
        }
        else if (created.Capacity.HasValue && created.Capacity > 0)
        {
            db.TicketTypes.Add(new TicketType
            {
                EventId    = created.Id,
                Name       = "General Registration",
                Price      = 0,
                Quantity   = created.Capacity.Value,
                Currency   = "EUR",
                SortOrder  = 0,
                MaxPerOrder = 10
            });
            await db.SaveChangesAsync(ct);
        }

        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("Moderator"))
        {
            await eventService.SubmitForApprovalAsync(created.Id, ct);
            await eventService.ApproveEventAsync(created.Id, currentUser.UserId!.Value, ct);
            await eventService.PublishEventAsync(created.Id, ct);
        }
        else
        {
            await eventService.SubmitForApprovalAsync(created.Id, ct);
        }

        TempData["Success"] = localizer["EventCreated"].Value;
        return RedirectToAction(nameof(Details), new { slug = created.Slug });
    }

    [HttpGet]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventByIdAsync(id, ct);
        if (ev == null) return NotFound();
        if (ev.OrganizerId != currentUser.UserId
            && !currentUser.IsInRole(UserRoles.Admin)
            && !currentUser.IsInRole(UserRoles.SuperAdmin))
            return Forbid();

        var vm = mapper.Map<EventEditViewModel>(ev);
        await PopulateDropdowns(vm, ct);

        vm.Status = ev.Status;

        var images = await db.EventImages
            .Where(i => i.EventId == id)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);
        vm.ExistingImages = mapper.Map<IReadOnlyList<EventImageViewModel>>(images);

        var ticketTypes = await db.TicketTypes
            .Where(t => t.EventId == id)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);
        vm.TicketTypeInputs = ticketTypes.Select(t => new TicketTypeInputViewModel
        {
            Name = t.Name,
            Quantity = t.Quantity, Description = t.Description
        }).ToList();

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventEditViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { await PopulateDropdowns(model, ct); return View(model); }

        var ev = await eventService.GetEventByIdAsync(model.Id, ct);
        if (ev == null) return NotFound();
        if (ev.OrganizerId != currentUser.UserId
            && !currentUser.IsInRole(UserRoles.Admin)
            && !currentUser.IsInRole(UserRoles.SuperAdmin))
            return Forbid();

        ev.Title            = model.Title;
        ev.Description      = model.Description;
        ev.ShortDescription = model.ShortDescription;
        ev.StartDate        = model.StartDate;
        ev.EndDate          = model.EndDate;
        ev.CategoryId       = model.CategoryId;
        ev.VenueId          = model.VenueId;
        ev.Capacity         = model.Capacity;
        ev.MinAge           = model.MinAge;
        ev.IsAccessible     = model.IsAccessible;
        ev.Latitude         = model.Latitude;
        ev.Longitude        = model.Longitude;

        ev.CoverImageUrl = model.ExistingCoverImageUrl;
        if (model.CoverImage != null)
        {
            await using var stream = model.CoverImage.OpenReadStream();
            ev.CoverImageUrl = await fileStorage.UploadAsync(
                stream, model.CoverImage.FileName, model.CoverImage.ContentType, ct);
        }

        var updated = await eventService.UpdateEventAsync(ev, model.SelectedTagIds, ct);

        var existingTt = await db.TicketTypes.Where(t => t.EventId == updated.Id).ToListAsync(ct);
        db.TicketTypes.RemoveRange(existingTt);
        int sort = 0;
        foreach (var tt in model.TicketTypeInputs.Where(t => !string.IsNullOrEmpty(t.Name)))
            db.TicketTypes.Add(new TicketType
            {
                EventId = updated.Id, Name = tt.Name, Price = 0,
                Quantity = tt.Quantity > 0 ? tt.Quantity : 0,
                Description = tt.Description, Currency = "EUR",
                SortOrder = sort++, MaxPerOrder = 10
            });
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["EventUpdated"].Value;
        return RedirectToAction(nameof(Details), new { slug = updated.Slug });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await eventService.DeleteEventAsync(id, ct);
        TempData["Success"] = localizer["EventDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForApproval(Guid id, CancellationToken ct = default)
    {
        var ev = await eventService.SubmitForApprovalAsync(id, ct);
        TempData["Success"] = localizer["EventSubmitted"].Value;
        return RedirectToAction(nameof(Details), new { slug = ev.Slug });
    }

    [HttpGet]
    [Authorize(Roles = "Moderator,Admin,SuperAdmin")]
    public IActionResult Pending(int page = 1)
        => RedirectToAction("Events", "Admin", new { filter = "pending" });

    [HttpPost]
    [Authorize(Roles = "Moderator,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
    {
        var adminId = currentUser.UserId!.Value;
        await eventService.ApproveEventAsync(id, adminId, ct);
        await eventService.PublishEventAsync(id, ct);
        TempData["Success"] = localizer["EventApproved"].Value;
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [Authorize(Roles = "Moderator,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reason, CancellationToken ct = default)
    {
        var adminId = currentUser.UserId!.Value;
        await eventService.RejectEventAsync(id, adminId, reason, ct);
        TempData["Error"] = localizer["EventRejected"].Value;
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImages(
        Guid eventId, List<IFormFile> images, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventByIdAsync(eventId, ct);
        if (ev == null) return NotFound();
        if (ev.OrganizerId != currentUser.UserId
            && !currentUser.IsInRole(UserRoles.Admin)
            && !currentUser.IsInRole(UserRoles.SuperAdmin))
            return Forbid();

        if (images == null || images.Count == 0)
        {
            TempData["Error"] = localizer["NoImages"].Value;
            return RedirectToAction(nameof(Edit), new { id = eventId });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        const long maxBytes = 5 * 1024 * 1024;

        var nextOrder = await db.EventImages
            .Where(i => i.EventId == eventId)
            .CountAsync(ct);

        foreach (var file in images)
        {
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                TempData["Error"] = localizer["InvalidImageType"].Value;
                return RedirectToAction(nameof(Edit), new { id = eventId });
            }
            if (file.Length > maxBytes)
            {
                TempData["Error"] = localizer["ImageTooLarge"].Value;
                return RedirectToAction(nameof(Edit), new { id = eventId });
            }

            await using var stream = file.OpenReadStream();
            var url = await fileStorage.UploadAsync(stream, file.FileName, file.ContentType, ct);

            db.EventImages.Add(new EventImage
            {
                EventId    = eventId,
                ImageUrl   = url,
                SortOrder  = nextOrder++,
                UploadedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        TempData["Success"] = localizer["ImageUploaded"].Value;
        return RedirectToAction(nameof(Edit), new { id = eventId });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(
        Guid imageId, CancellationToken ct = default)
    {
        var image = await db.EventImages
            .Include(i => i.Event)
            .FirstOrDefaultAsync(i => i.Id == imageId, ct);
        if (image == null) return NotFound();

        if (image.Event.OrganizerId != currentUser.UserId
            && !currentUser.IsInRole(UserRoles.Admin)
            && !currentUser.IsInRole(UserRoles.SuperAdmin))
            return Forbid();

        db.EventImages.Remove(image);
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["ImageDeleted"].Value;
        return RedirectToAction(nameof(Edit), new { id = image.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Series(Guid seriesId, CancellationToken ct = default)
    {
        var series = await eventService.GetSeriesAsync(seriesId, ct);
        if (series == null) return NotFound();

        var events = await eventService.GetSeriesEventsAsync(seriesId, ct);

        return View(new SeriesViewModel
        {
            SeriesId        = series.Id,
            Title           = series.Title,
            RecurrenceRule  = series.RecurrenceRule,
            Events          = mapper.Map<IReadOnlyList<EventCardViewModel>>(events)
        });
    }

    [HttpGet]
    public IActionResult Calendar() => View();

    [HttpGet]
    public async Task<IActionResult> CalendarEvents(
        DateTime start, DateTime end, CancellationToken ct = default)
    {
        static string CategoryColor(string name) => name.ToLowerInvariant() switch
        {
            var n when n.Contains("concert") || n.Contains("концерт") => "#6366f1",
            var n when n.Contains("workshop") || n.Contains("уъркшоп") || n.Contains("обучение") => "#059669",
            var n when n.Contains("sport") || n.Contains("спорт") => "#d97706",
            var n when n.Contains("art") || n.Contains("cultur") || n.Contains("изкуств") || n.Contains("култур") => "#db2777",
            var n when n.Contains("volunteer") || n.Contains("доброволч") => "#0891b2",
            _ => "#71717a"
        };

        var events = await eventService.GetEventsInRangeAsync(start, end, ct);
        var result = events.Select(e => new
        {
            id    = e.Id,
            title = e.Title,
            start = e.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            end   = e.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            url   = Url.Action(nameof(Details), new { slug = e.Slug }),
            color = CategoryColor(e.Category?.Name ?? string.Empty),
            allDay = false
        });
        return Json(result);
    }

    [HttpGet]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin,Moderator")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventByIdAsync(id, ct);
        if (ev == null) return NotFound();

        var uid = currentUser.UserId!.Value;
        if (ev.OrganizerId != uid
            && !currentUser.IsInRole(UserRoles.Admin)
            && !currentUser.IsInRole(UserRoles.SuperAdmin)
            && !currentUser.IsInRole(UserRoles.Moderator))
            return Forbid();

        var checkedIn = await db.Tickets
            .CountAsync(t => t.TicketType.EventId == id && t.Status == TicketStatus.CheckedIn, ct);
        var total = await db.Tickets
            .CountAsync(t => t.TicketType.EventId == id, ct);

        return View(new CheckInViewModel
        {
            EventId        = ev.Id,
            EventTitle     = ev.Title,
            EventSlug      = ev.Slug,
            EventDate      = ev.StartDate,
            VenueName      = ev.Venue?.Name,
            CheckedInCount = checkedIn,
            TotalTickets   = total
        });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin,Moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInTicket(
        Guid eventId, string qrCode, CancellationToken ct = default)
    {
        try
        {
            var ticket = await ticketService.CheckInTicketAsync(qrCode, currentUser.UserId!.Value, ct);

            var holderProfile = await db.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == ticket.HolderId, ct);

            var checkedIn = await db.Tickets
                .CountAsync(t => t.TicketType.EventId == eventId && t.Status == TicketStatus.CheckedIn, ct);

            return Json(new
            {
                success        = true,
                ticketNumber   = ticket.TicketNumber,
                holderName     = holderProfile?.FullName ?? "—",
                checkedInCount = checkedIn
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterForEvent(
        Guid eventId, Guid ticketTypeId, int quantity = 1, CancellationToken ct = default)
    {
        if (!User.CanParticipate())
        {
            TempData["Error"] = localizer["AdminsCannotRegisterForEvents"].Value;
            var ev0 = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
            return RedirectToAction(nameof(Details), new { slug = ev0?.Slug });
        }

        try
        {
            await ticketService.RegisterForEventAsync(
                eventId, currentUser.UserId!.Value, ticketTypeId, quantity, ct);

            TempData["Success"] = localizer["RegistrationSuccess"].Value;
            var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
            return RedirectToAction(nameof(Details), new { slug = ev?.Slug });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
            return RedirectToAction(nameof(Details), new { slug = ev?.Slug });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RateEvent(
        Guid eventId, int score, string? reviewText = null, CancellationToken ct = default)
    {
        await ratingService.RateEventAsync(eventId, currentUser.UserId!.Value, score, reviewText, ct);
        TempData["Success"] = localizer["EventRated"].Value;
        var ev = await eventService.GetEventByIdAsync(eventId, ct);
        return RedirectToAction(nameof(Details), new { slug = ev?.Slug });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        Guid eventId, string content, Guid? parentCommentId = null, CancellationToken ct = default)
    {
        if (!User.CanParticipate())
        {
            TempData["Error"] = localizer["AdminsCannotParticipate"].Value;
            var ev0 = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
            return RedirectToAction(nameof(Details), new { slug = ev0?.Slug ?? "" });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = localizer["CommentCannotBeEmpty"].Value;
            var ev0 = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
            return RedirectToAction(nameof(Details), new { slug = ev0?.Slug ?? "" });
        }

        var comment = new Comment
        {
            Id              = Guid.NewGuid(),
            EventId         = eventId,
            UserId          = currentUser.UserId!.Value,
            Content         = content.Trim(),
            ParentCommentId = parentCommentId,
            IsHidden        = false,
            IsEdited        = false,
            UpvoteCount     = 0,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = User.Identity?.Name
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["CommentPosted"].Value;
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
        return RedirectToAction(nameof(Details), new { slug = ev?.Slug ?? "" });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAttendance(
        Guid eventId, AttendanceStatus status, CancellationToken ct = default)
    {
        var userId = currentUser.UserId!.Value;
        var existing = await db.EventAttendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.EventId == eventId, ct);

        if (existing != null)
        {
            if (existing.Status == status)
                db.EventAttendances.Remove(existing);
            else
                existing.Status = status;
        }
        else
        {
            db.EventAttendances.Add(new EventAttendance
            {
                UserId    = userId,
                EventId   = eventId,
                Status    = status,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        var ev = await eventService.GetEventByIdAsync(eventId, ct);
        return RedirectToAction(nameof(Details), new { slug = ev?.Slug });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(Guid eventId, CancellationToken ct = default)
    {
        if (!User.CanParticipate())
        {
            TempData["Error"] = localizer["AdminsCannotParticipate"].Value;
            var ev0 = await eventService.GetEventByIdAsync(eventId, ct);
            return RedirectToAction(nameof(Details), new { slug = ev0?.Slug });
        }

        var userId = currentUser.UserId!.Value;
        var existing = await db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.EventId == eventId, ct);

        if (existing != null)
            db.Favorites.Remove(existing);
        else
            db.Favorites.Add(new Favorite { UserId = userId, EventId = eventId, CreatedAt = DateTime.UtcNow });

        await db.SaveChangesAsync(ct);
        var ev = await eventService.GetEventByIdAsync(eventId, ct);
        return RedirectToAction(nameof(Details), new { slug = ev?.Slug });
    }

    private static string BuildRrule(EventCreateViewModel model)
    {
        var sb = new System.Text.StringBuilder(
            $"FREQ={model.RruleFreq.ToUpperInvariant()};COUNT={model.RruleCount}");

        if (model.RruleFreq.Equals("WEEKLY", StringComparison.OrdinalIgnoreCase)
            && model.RruleByDay.Count > 0)
            sb.Append($";BYDAY={string.Join(",", model.RruleByDay)}");

        if (model.RruleFreq.Equals("MONTHLY", StringComparison.OrdinalIgnoreCase))
            sb.Append($";BYMONTHDAY={model.RruleByMonthDay}");

        return sb.ToString();
    }

    private async Task PopulateDropdowns(EventCreateViewModel model, CancellationToken ct = default)
    {
        model.Categories = await db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            .ToListAsync(ct);

        model.Venues = await db.Venues
            .OrderBy(v => v.Name)
            .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = $"{v.Name}, {v.City}" })
            .ToListAsync(ct);

        model.Tags = await db.Tags
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
            .ToListAsync(ct);
    }

}
