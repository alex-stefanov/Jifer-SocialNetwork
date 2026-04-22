using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface IEventService
{
    Task<Event> CreateEventAsync(Event eventEntity, IEnumerable<int>? tagIds = null, CancellationToken ct = default);
    Task<Event> UpdateEventAsync(Event eventEntity, IEnumerable<int>? tagIds = null, CancellationToken ct = default);
    Task DeleteEventAsync(Guid eventId, CancellationToken ct = default);
    Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken ct = default);
    Task<Event?> GetEventBySlugAsync(string slug, CancellationToken ct = default);

    Task<(IReadOnlyList<Event> Items, int TotalCount)> GetEventsPagedAsync(
        int page, int pageSize, int? categoryId = null, EventStatus? status = null,
        bool? isFree = null, DateTime? fromDate = null, DateTime? toDate = null,
        string? searchQuery = null, string? tag = null, bool? hasVolunteerTasks = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Event>> GetUpcomingEventsAsync(int count = 10, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetNearbyEventsAsync(decimal lat, decimal lng, double radiusKm, int count = 20, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetEventsInRangeAsync(DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetOrganizerEventsAsync(Guid organizerId, int count = 6, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken ct = default);

    Task<Event> SubmitForApprovalAsync(Guid eventId, CancellationToken ct = default);
    Task<Event> ApproveEventAsync(Guid eventId, Guid approvedById, CancellationToken ct = default);
    Task<Event> RejectEventAsync(Guid eventId, Guid rejectedById, string reason, CancellationToken ct = default);
    Task<Event> PublishEventAsync(Guid eventId, CancellationToken ct = default);
    Task<Event> CancelEventAsync(Guid eventId, string? reason = null, CancellationToken ct = default);

    Task IncrementViewCountAsync(Guid eventId, CancellationToken ct = default);
    Task<double> GetAverageRatingAsync(Guid eventId, CancellationToken ct = default);

    Task<EventSeries> CreateRecurringEventAsync(Event templateEvent, string rrule, int maxOccurrences = 52, CancellationToken ct = default);
    Task<EventSeries?> GetSeriesAsync(Guid seriesId, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetSeriesEventsAsync(Guid seriesId, CancellationToken ct = default);
}
