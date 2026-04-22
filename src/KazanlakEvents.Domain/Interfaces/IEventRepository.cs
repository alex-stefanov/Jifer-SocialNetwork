using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Interfaces;

public interface IEventRepository : IRepository<Event>
{
    Task<Event?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetUpcomingEventsAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetByCategoryAsync(int categoryId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetByStatusAsync(EventStatus status, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetNearbyEventsAsync(decimal latitude, decimal longitude, double radiusKm, int count, CancellationToken ct = default);
    Task<Event?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(EventStatus? status = null, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
}
