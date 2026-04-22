using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Notification;

public class NotificationItemViewModel
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public string IconClass => Type switch
    {
        NotificationType.EventApproved   => "bi-calendar-check text-success",
        NotificationType.EventRejected   => "bi-calendar-x text-danger",
        NotificationType.EventCancelled  => "bi-calendar-x text-warning",
        NotificationType.EventReminder   => "bi-calendar-event text-primary",
        NotificationType.TicketPurchased => "bi-ticket-perforated text-success",
        NotificationType.NewFollower     => "bi-person-plus text-info",
        NotificationType.NewComment      => "bi-chat-left-text text-secondary",
        NotificationType.NewRating       => "bi-star-fill text-warning",
        _                                => "bi-bell text-muted"
    };

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.UtcNow - CreatedAt;
            if (diff.TotalMinutes < 1)   return "Just now";
            if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours   < 24)  return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays    < 2)   return "Yesterday";
            if (diff.TotalDays    < 7)   return $"{(int)diff.TotalDays}d ago";
            return CreatedAt.ToString("dd.MM.yyyy");
        }
    }
}
