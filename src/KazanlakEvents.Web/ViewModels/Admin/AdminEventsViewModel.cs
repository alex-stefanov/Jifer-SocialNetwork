namespace KazanlakEvents.Web.ViewModels.Admin;

public class AdminEventsViewModel
{
    public List<PendingEventItem> Events { get; set; } = new();
    public string? Filter { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
