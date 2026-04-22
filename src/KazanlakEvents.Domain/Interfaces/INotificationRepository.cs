using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId, int page, int pageSize, IEnumerable<NotificationType>? types = null, bool unreadOnly = false, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(Guid userId, IEnumerable<NotificationType>? types = null, bool unreadOnly = false, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
