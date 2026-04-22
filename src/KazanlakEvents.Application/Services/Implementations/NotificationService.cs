using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Services.Implementations;

public class NotificationService(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendNotificationAsync(
        Guid userId, NotificationType type, string title, string message,
        string? linkUrl = null, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await notificationRepository.AddAsync(notification, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogDebug("Sent {Type} notification to user {UserId}", type, userId);
    }

    public async Task SendBulkNotificationAsync(
        IEnumerable<Guid> userIds, NotificationType type, string title, string message,
        string? linkUrl = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var notifications = userIds.Select(uid => new Notification
        {
            UserId = uid,
            Type = type,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAt = now
        });

        await notificationRepository.AddRangeAsync(notifications, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogDebug("Sent bulk {Type} notification to {Count} users", type, notifications.Count());
    }

    public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(
        Guid userId, int page = 1, int pageSize = 20, string? filter = null, CancellationToken ct = default)
    {
        var (types, unreadOnly) = ParseFilter(filter);
        return await notificationRepository.GetByUserAsync(userId, page, pageSize, types, unreadOnly, ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await notificationRepository.GetUnreadCountAsync(userId, ct);

    public async Task<int> GetTotalCountAsync(Guid userId, string? filter = null, CancellationToken ct = default)
    {
        var (types, unreadOnly) = ParseFilter(filter);
        return await notificationRepository.GetTotalCountAsync(userId, types, unreadOnly, ct);
    }

    private static (NotificationType[]? types, bool unreadOnly) ParseFilter(string? filter) => filter switch
    {
        "unread"  => (null, true),
        "events"  => (new[] { NotificationType.EventApproved, NotificationType.EventRejected, NotificationType.EventReminder, NotificationType.EventCancelled }, false),
        "tickets" => (new[] { NotificationType.TicketPurchased }, false),
        "social"  => (new[] { NotificationType.NewFollower, NotificationType.NewComment, NotificationType.NewRating }, false),
        _         => (null, false)
    };

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId, ct)
            ?? throw new NotFoundException(nameof(Notification), notificationId);

        notification.IsRead = true;
        notificationRepository.Update(notification);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
        => await notificationRepository.MarkAllAsReadAsync(userId, ct);
}
