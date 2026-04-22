namespace KazanlakEvents.Web.ViewModels.Notification;

public class NotificationListViewModel
{
    public IReadOnlyList<NotificationItemViewModel> Notifications { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int UnreadCount { get; set; }
    public string ActiveFilter { get; set; } = string.Empty;
}
