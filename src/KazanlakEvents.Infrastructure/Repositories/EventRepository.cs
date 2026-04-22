using System.Linq.Expressions;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Infrastructure.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    private readonly ApplicationDbContext _context;

    // All queries must exclude soft-deleted events (no global query filter — handled here).
    private IQueryable<Event> Active => _dbSet.Where(e => !e.IsDeleted);

    public EventRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    // Override base methods so callers that use the generic IRepository<Event> interface
    // still get soft-delete filtering even though the global query filter was removed.

    public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Active.FirstOrDefaultAsync(e => e.Id == id, ct);

    public override async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken ct = default)
        => await Active.AsNoTracking().ToListAsync(ct);

    public override async Task<IReadOnlyList<Event>> FindAsync(
        Expression<Func<Event, bool>> predicate, CancellationToken ct = default)
        => await Active.AsNoTracking().Where(predicate).ToListAsync(ct);

    public override IQueryable<Event> Query() => Active;

    public override async Task<bool> AnyAsync(
        Expression<Func<Event, bool>> predicate, CancellationToken ct = default)
        => await Active.AnyAsync(predicate, ct);

    public override async Task<int> CountAsync(
        Expression<Func<Event, bool>>? predicate = null, CancellationToken ct = default)
        => predicate == null
            ? await Active.CountAsync(ct)
            : await Active.CountAsync(predicate, ct);

    public async Task<Event?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await Active
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.EventTags).ThenInclude(et => et.Tag)
            .Include(e => e.Images)
            .FirstOrDefaultAsync(e => e.Slug == slug, ct);

    public async Task<IReadOnlyList<Event>> GetUpcomingEventsAsync(int count, CancellationToken ct = default)
        => await Active
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow)
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .OrderBy(e => e.StartDate)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken ct = default)
        => await Active
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId)
            .Include(e => e.Category)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetByCategoryAsync(
        int categoryId, int page, int pageSize, CancellationToken ct = default)
        => await Active
            .AsNoTracking()
            .Where(e => e.CategoryId == categoryId && e.Status == EventStatus.Published)
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .OrderBy(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetByStatusAsync(
        EventStatus status, int page, int pageSize, CancellationToken ct = default)
        => await Active
            .AsNoTracking()
            .Where(e => e.Status == status)
            .Include(e => e.Category)
            .OrderByDescending(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> SearchAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
    {
        var pattern = $"%{query}%";
        return await Active
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published
                && (EF.Functions.Like(e.Title, pattern)
                    || EF.Functions.Like(e.Description, pattern)
                    || EF.Functions.Like(e.ShortDescription ?? "", pattern)))
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .OrderBy(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Event>> GetNearbyEventsAsync(
        decimal latitude, decimal longitude, double radiusKm, int count, CancellationToken ct = default)
    {
        // Bounding-box pre-filter in SQL, then exact order by distance in memory
        var latDelta = (decimal)(radiusKm / 111.32);
        var lngDelta = (decimal)(radiusKm / (111.32 * Math.Cos((double)latitude * Math.PI / 180.0)));

        var candidates = await Active
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published
                && e.Latitude.HasValue && e.Longitude.HasValue
                && e.Latitude >= latitude - latDelta && e.Latitude <= latitude + latDelta
                && e.Longitude >= longitude - lngDelta && e.Longitude <= longitude + lngDelta)
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .ToListAsync(ct);

        return candidates
            .OrderBy(e => Math.Sqrt(
                Math.Pow((double)(e.Latitude!.Value - latitude), 2) +
                Math.Pow((double)(e.Longitude!.Value - longitude), 2)))
            .Take(count)
            .ToList();
    }

    public async Task<Event?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Active
            .AsNoTracking()
            .AsSplitQuery()   // 9 collection includes — split avoids Cartesian explosion
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.Series)
            .Include(e => e.EventTags).ThenInclude(et => et.Tag)
            .Include(e => e.Images)
            .Include(e => e.TicketTypes)
            .Include(e => e.Comments)
            .Include(e => e.Ratings)
            .Include(e => e.Attendances)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<int> GetTotalCountAsync(EventStatus? status = null, CancellationToken ct = default)
        => status.HasValue
            ? await Active.CountAsync(e => e.Status == status.Value, ct)
            : await Active.CountAsync(ct);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => await Active.AnyAsync(e => e.Slug == slug, ct);
}
