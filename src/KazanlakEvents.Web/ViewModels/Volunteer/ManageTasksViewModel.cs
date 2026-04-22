namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class ManageTasksViewModel
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventSlug { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public IReadOnlyList<ManageTaskViewModel> Tasks { get; set; } = [];
}
