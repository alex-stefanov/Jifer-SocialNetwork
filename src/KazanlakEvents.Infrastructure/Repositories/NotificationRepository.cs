using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(
        Guid userId, int page, int pageSize,
        IEnumerable<NotificationType>? types = null, bool unreadOnly = false,
        CancellationToken ct = default)
    {
        var q = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly) q = q.Where(n => !n.IsRead);
        if (types != null)
        {
            var typeList = types.ToList();
            q = q.Where(n => typeList.Contains(n.Type));
        }
        return await q.OrderByDescending(n => n.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<int> GetTotalCountAsync(
        Guid userId,
        IEnumerable<NotificationType>? types = null, bool unreadOnly = false,
        CancellationToken ct = default)
    {
        var q = _context.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly) q = q.Where(n => !n.IsRead);
        if (types != null)
        {
            var typeList = types.ToList();
            q = q.Where(n => typeList.Contains(n.Type));
        }
        return await q.CountAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
}
