namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class EventTasksViewModel
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventSlug { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public IReadOnlyList<VolunteerTaskViewModel> Tasks { get; set; } = [];
}
