using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Services.Implementations;

public class EventService(
    IEventRepository eventRepository,
    IRepository<EventSeries> seriesRepository,
    IUnitOfWork unitOfWork,
    ISlugService slugService,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    IWebhookService webhookService,
    IHtmlSanitizerService htmlSanitizer,
    ILogger<EventService> logger) : IEventService
{
    public async Task<Event> CreateEventAsync(
        Event eventEntity, IEnumerable<int>? tagIds = null, CancellationToken ct = default)
    {
        var organizerId = currentUser.UserId
            ?? throw new ForbiddenAccessException();

        eventEntity.Slug = await slugService.GenerateUniqueSlugAsync<Event>(
            eventEntity.Title,
            slug => eventRepository.SlugExistsAsync(slug, ct),
            ct);

        eventEntity.OrganizerId = organizerId;
        eventEntity.Status = EventStatus.Draft;
        eventEntity.ViewCount = 0;
        eventEntity.Description = htmlSanitizer.Sanitize(eventEntity.Description);

        await eventRepository.AddAsync(eventEntity, ct);

        if (tagIds != null)
        {
            foreach (var tagId in tagIds.Distinct())
                eventEntity.EventTags.Add(new EventTag { TagId = tagId });
        }

        await unitOfWork.SaveChangesAsync(ct);

        await webhookService.DispatchAsync("event.created", new
        {
            eventId = eventEntity.Id,
            title   = eventEntity.Title,
            status  = eventEntity.Status.ToString()
        }, ct);

        logger.LogInformation(
            "Event {EventId} '{Title}' created by user {UserId}",
            eventEntity.Id, eventEntity.Title, organizerId);

        return eventEntity;
    }

    public async Task<Event> UpdateEventAsync(
        Event eventEntity, IEnumerable<int>? tagIds = null, CancellationToken ct = default)
    {
        var existing = await eventRepository.GetByIdAsync(eventEntity.Id, ct)
            ?? throw new NotFoundException(nameof(Event), eventEntity.Id);

        var isAdminOrAbove = currentUser.IsInRole(UserRoles.Admin)
                          || currentUser.IsInRole(UserRoles.SuperAdmin);

        if (existing.OrganizerId != currentUser.UserId && !isAdminOrAbove)
            throw new ForbiddenAccessException();

        if (!string.Equals(existing.Title, eventEntity.Title, StringComparison.Ordinal))
        {
            existing.Slug = await slugService.GenerateUniqueSlugAsync<Event>(
                eventEntity.Title,
                slug => eventRepository.SlugExistsAsync(slug, ct),
                ct);
        }

        existing.Title = eventEntity.Title;
        existing.Description = htmlSanitizer.Sanitize(eventEntity.Description);
        existing.ShortDescription = eventEntity.ShortDescription;
        existing.StartDate = eventEntity.StartDate;
        existing.EndDate = eventEntity.EndDate;
        existing.VenueId = eventEntity.VenueId;
        existing.CategoryId = eventEntity.CategoryId;
        existing.Capacity = eventEntity.Capacity;
        existing.IsFree = eventEntity.IsFree;
        existing.IsAccessible = eventEntity.IsAccessible;
        existing.MinAge = eventEntity.MinAge;
        existing.CoverImageUrl = eventEntity.CoverImageUrl;
        existing.Latitude = eventEntity.Latitude;
        existing.Longitude = eventEntity.Longitude;

        if (tagIds != null)
        {
            existing.EventTags.Clear();
            foreach (var tagId in tagIds.Distinct())
                existing.EventTags.Add(new EventTag { EventId = existing.Id, TagId = tagId });
        }

        eventRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Event {EventId} updated by user {UserId}", existing.Id, currentUser.UserId);

        return existing;
    }

    public async Task DeleteEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var existing = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(Event), eventId);

        var isAdminOrAbove = currentUser.IsInRole(UserRoles.Admin)
                          || currentUser.IsInRole(UserRoles.SuperAdmin);

        if (existing.OrganizerId != currentUser.UserId && !isAdminOrAbove)
            throw new ForbiddenAccessException();

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;
        existing.DeletedBy = currentUser.UserName ?? currentUser.UserId.ToString();

        eventRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Event {EventId} soft-deleted by user {UserId}", eventId, currentUser.UserId);
    }

    public async Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken ct = default)
        => await eventRepository.GetWithDetailsAsync(eventId, ct);

    public async Task<Event?> GetEventBySlugAsync(string slug, CancellationToken ct = default)
        => await eventRepository.GetBySlugAsync(slug, ct);

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetEventsPagedAsync(
        int page, int pageSize,
        int? categoryId = null, EventStatus? status = null,
        bool? isFree = null, DateTime? fromDate = null, DateTime? toDate = null,
        string? searchQuery = null, string? tag = null, bool? hasVolunteerTasks = null,
        CancellationToken ct = default)
    {
        IQueryable<Event> query = eventRepository.Query()
            .AsNoTracking()
            .Where(e => e.Status == (status ?? EventStatus.Published));

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (isFree.HasValue)
            query = query.Where(e => e.IsFree == isFree.Value);

        if (fromDate.HasValue)
            query = query.Where(e => e.StartDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.EndDate <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var pattern = $"%{searchQuery}%";
            query = query.Where(e =>
                EF.Functions.Like(e.Title, pattern) ||
                EF.Functions.Like(e.Description, pattern) ||
                EF.Functions.Like(e.ShortDescription ?? "", pattern));
        }

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(e => e.EventTags.Any(et => et.Tag.Name == tag));

        if (hasVolunteerTasks == true)
            query = query.Where(e => e.VolunteerTasks.Any());

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.Ratings)
            .Include(e => e.Attendances)
            .OrderBy(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Event>> GetUpcomingEventsAsync(
        int count = 10, CancellationToken ct = default)
        => await eventRepository.GetUpcomingEventsAsync(count, ct);

    public async Task<IReadOnlyList<Event>> GetNearbyEventsAsync(
        decimal lat, decimal lng, double radiusKm, int count = 20, CancellationToken ct = default)
        => await eventRepository.GetNearbyEventsAsync(lat, lng, radiusKm, count, ct);

    public async Task<IReadOnlyList<Event>> GetEventsInRangeAsync(
        DateTime start, DateTime end, CancellationToken ct = default)
        => await eventRepository.Query()
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published
                     && e.StartDate < end
                     && e.EndDate > start)
            .Include(e => e.Category)
            .OrderBy(e => e.StartDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetOrganizerEventsAsync(
        Guid organizerId, int count = 6, CancellationToken ct = default)
        => await eventRepository.Query()
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId && e.Status == EventStatus.Published)
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.Attendances)
            .Include(e => e.Ratings)
            .Include(e => e.Comments)
            .Include(e => e.TicketTypes)
            .OrderByDescending(e => e.StartDate)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(
        Guid organizerId, CancellationToken ct = default)
        => await eventRepository.Query()
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId)
            .Include(e => e.TicketTypes)
            .Include(e => e.Orders)
            .Include(e => e.Attendances)
            .Include(e => e.VolunteerTasks)
                .ThenInclude(t => t.Shifts)
                    .ThenInclude(s => s.Signups)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(ct);

    public async Task<Event> SubmitForApprovalAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(Event), eventId);

        if (ev.Status != EventStatus.Draft)
            throw new InvalidOperationException(
                $"Only Draft events can be submitted for approval. Current status: {ev.Status}");

        ev.Status = EventStatus.PendingApproval;
        eventRepository.Update(ev);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Event {EventId} submitted for approval by organizer {OrganizerId}",
            eventId, ev.OrganizerId);

        return ev;
    }

    public async Task<Event> ApproveEventAsync(
        Guid eventId, Guid approvedById, CancellationToken ct = default)
    {
        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(Event), eventId);

        if (ev.Status != EventStatus.PendingApproval)
            throw new InvalidOperationException(
                $"Only PendingApproval events can be approved. Current status: {ev.Status}");

        ev.Status = EventStatus.Approved;
        ev.ApprovedById = approvedById;
        ev.ApprovedAt = DateTime.UtcNow;

        eventRepository.Update(ev);
        await unitOfWork.SaveChangesAsync(ct);

        await webhookService.DispatchAsync("event.approved", new
        {
            eventId = ev.Id,
            title   = ev.Title,
            status  = ev.Status.ToString()
        }, ct);

        await notificationService.SendNotificationAsync(
            ev.OrganizerId,
            NotificationType.EventApproved,
            "Event approved",
            $"Your event '{ev.Title}' has been approved and is ready to publish.",
            linkUrl: $"/Event/Details/{ev.Slug}",
            ct: ct);

        logger.LogInformation(
            "Event {EventId} approved by {ApprovedById}", eventId, approvedById);

        return ev;
    }

    public async Task<Event> RejectEventAsync(
        Guid eventId, Guid rejectedById, string reason, CancellationToken ct = default)
    {
        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(Event), eventId);

        if (ev.Status != EventStatus.PendingApproval)
            throw new InvalidOperationException(
                $"Only PendingApproval events can be rejected. Current status: {ev.Status}");

        ev.Status = EventStatus.Rejected;
        ev.RejectionReason = reason;

        eventRepository.Update(ev);
        await unitOfWork.SaveChangesAsync(ct);

        await notificationService.SendNotificationAsync(
            ev.OrganizerId,
            NotificationType.EventRejected,
            "Event rejected",
            $"Your event '{ev.Title}' was not approved. Reason: {reason}",
            linkUrl: "/Event/MyEvents",
            ct: ct);

        logger.LogInformation(
            "Event {EventId} rejected by {RejectedById}. Reason: {Reason}",
            eventId, rejectedById, reason);

        return ev;
    }

    public async Task<Event> PublishEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(Event), eventId);

        if (ev.Status != EventStatus.Approved)
            throw new InvalidOperationException(
                $"Only Approved events can be published. Current status: {ev.Status}");

        ev.Status = EventStatus.Published;
        eventRepository.Update(ev);
        await unitOfWork.SaveChangesAsync(ct);

        await webhookService.DispatchAsync("event.published", new
        {
            eventId = ev.Id,
            title   = ev.Title,
            status  = ev.Status.ToString()
        }, ct);

        logger.LogInformation("Event {EventId} published", eventId);

        return ev;
    }

    public async Task<Event> CancelEventAsync(
        Guid eventId, string? reason = null, CancellationToken ct = default)
    {
        var ev = await eventRepository.GetWithDetailsAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(Event), eventId);

        ev.Status = EventStatus.Cancelled;
        if (reason != null)
            ev.RejectionReason = reason;

        eventRepository.Update(ev);
        await unitOfWork.SaveChangesAsync(ct);

        await webhookService.DispatchAsync("event.cancelled", new
        {
            eventId = ev.Id,
            title   = ev.Title,
            status  = ev.Status.ToString(),
            reason
        }, ct);

        var attendeeIds = ev.Attendances.Select(a => a.UserId).ToList();
        if (attendeeIds.Count > 0)
        {
            await notificationService.SendBulkNotificationAsync(
                attendeeIds,
                NotificationType.EventCancelled,
                "Event cancelled",
                $"The event '{ev.Title}' has been cancelled." +
                (reason != null ? $" Reason: {reason}" : string.Empty),
                linkUrl: $"/Event/Details/{ev.Slug}",
                ct: ct);
        }

        logger.LogInformation(
            "Event {EventId} cancelled. {AttendeeCount} attendees notified.",
            eventId, attendeeIds.Count);

        return ev;
    }

    public async Task IncrementViewCountAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await eventRepository.GetByIdAsync(eventId, ct);
        if (ev == null) return;
        ev.ViewCount++;
        eventRepository.Update(ev);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<double> GetAverageRatingAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await eventRepository.GetWithDetailsAsync(eventId, ct);
        if (ev == null || ev.Ratings.Count == 0)
            return 0;

        return ev.Ratings.Average(r => r.Score);
    }

    public async Task<EventSeries> CreateRecurringEventAsync(
        Event templateEvent, string rrule, int maxOccurrences = 52, CancellationToken ct = default)
    {
        var organizerId = currentUser.UserId ?? throw new ForbiddenAccessException();

        var series = await seriesRepository.AddAsync(new EventSeries
        {
            Title          = templateEvent.Title,
            Description    = templateEvent.ShortDescription,
            RecurrenceRule = rrule,
            OrganizerId    = organizerId
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);   // persist to get series.Id

        var duration    = templateEvent.EndDate - templateEvent.StartDate;
        var occurrences = GenerateOccurrences(templateEvent.StartDate, rrule, maxOccurrences).ToList();

        foreach (var startDate in occurrences)
        {
            var ev = new Event
            {
                Title            = templateEvent.Title,
                Description      = templateEvent.Description,
                ShortDescription = templateEvent.ShortDescription,
                OrganizerId      = organizerId,
                VenueId          = templateEvent.VenueId,
                CategoryId       = templateEvent.CategoryId,
                CoverImageUrl    = templateEvent.CoverImageUrl,
                StartDate        = startDate,
                EndDate          = startDate + duration,
                Capacity         = templateEvent.Capacity,
                IsFree           = templateEvent.IsFree,
                IsAccessible     = templateEvent.IsAccessible,
                MinAge           = templateEvent.MinAge,
                Latitude         = templateEvent.Latitude,
                Longitude        = templateEvent.Longitude,
                Status           = EventStatus.Draft,
                ViewCount        = 0,
                SeriesId         = series.Id
            };

            ev.Slug = await slugService.GenerateUniqueSlugAsync<Event>(
                $"{templateEvent.Title}-{startDate:yyyy-MM-dd}",
                slug => eventRepository.SlugExistsAsync(slug, ct),
                ct);

            await eventRepository.AddAsync(ev, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created recurring series {SeriesId} '{Title}' with {Count} occurrences",
            series.Id, series.Title, occurrences.Count);

        return series;
    }

    public async Task<EventSeries?> GetSeriesAsync(Guid seriesId, CancellationToken ct = default)
        => await seriesRepository.GetByIdAsync(seriesId, ct);

    public async Task<IReadOnlyList<Event>> GetSeriesEventsAsync(
        Guid seriesId, CancellationToken ct = default)
        => await eventRepository.Query()
            .Where(e => e.SeriesId == seriesId)
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.Attendances)
            .Include(e => e.Ratings)
            .OrderBy(e => e.StartDate)
            .ToListAsync(ct);

    private static readonly Dictionary<string, DayOfWeek> DayCodeMap = new()
    {
        ["MO"] = DayOfWeek.Monday,   ["TU"] = DayOfWeek.Tuesday,
        ["WE"] = DayOfWeek.Wednesday, ["TH"] = DayOfWeek.Thursday,
        ["FR"] = DayOfWeek.Friday,   ["SA"] = DayOfWeek.Saturday,
        ["SU"] = DayOfWeek.Sunday
    };

    private static IEnumerable<DateTime> GenerateOccurrences(DateTime start, string rrule, int max)
    {
        var parts = rrule.Split(';')
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim().ToUpperInvariant(), p => p[1].Trim().ToUpperInvariant());

        if (!parts.TryGetValue("FREQ", out var freq)) yield break;

        int count = max;
        if (parts.TryGetValue("COUNT", out var countStr) && int.TryParse(countStr, out var parsed))
            count = Math.Min(parsed, max);

        switch (freq)
        {
            case "DAILY":
            {
                for (int i = 0; i < count; i++)
                    yield return start.AddDays(i);
                break;
            }
            case "WEEKLY":
            {
                var days = new HashSet<DayOfWeek>();
                if (parts.TryGetValue("BYDAY", out var byDay))
                    foreach (var code in byDay.Split(','))
                        if (DayCodeMap.TryGetValue(code.Trim(), out var dow))
                            days.Add(dow);

                if (days.Count == 0) days.Add(start.DayOfWeek);

                int emitted = 0;
                var cursor = start.Date;
                while (emitted < count)
                {
                    if (days.Contains(cursor.DayOfWeek))
                    {
                        yield return cursor.Add(start.TimeOfDay);
                        emitted++;
                    }
                    cursor = cursor.AddDays(1);
                }
                break;
            }
            case "MONTHLY":
            {
                int targetDay = start.Day;
                if (parts.TryGetValue("BYMONTHDAY", out var bmd) && int.TryParse(bmd, out var bmdVal))
                    targetDay = bmdVal;

                for (int i = 0; i < count; i++)
                {
                    var monthStart = new DateTime(start.Year, start.Month, 1).AddMonths(i);
                    int day = Math.Min(targetDay, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
                    yield return new DateTime(monthStart.Year, monthStart.Month, day,
                        start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                }
                break;
            }
        }
    }
}

