namespace KazanlakEvents.Web.ViewModels.Event;

public class EventListViewModel
{
    public IReadOnlyList<EventCardViewModel> Events { get; set; } = new List<EventCardViewModel>();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int? CategoryId { get; set; }
    public string? SearchQuery { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Tag { get; set; }
    public bool? HasVolunteerTasks { get; set; }
    public IReadOnlyList<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
    public int UpcomingCount { get; set; }
}
