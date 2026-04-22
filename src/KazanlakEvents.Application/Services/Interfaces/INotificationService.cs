using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message, string? linkUrl = null, CancellationToken ct = default);
    Task SendBulkNotificationAsync(IEnumerable<Guid> userIds, NotificationType type, string title, string message, string? linkUrl = null, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20, string? filter = null, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(Guid userId, string? filter = null, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
