namespace KazanlakEvents.Web.ViewModels.Event;

public class CheckInViewModel
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventSlug { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? VenueName { get; set; }
    public int CheckedInCount { get; set; }
    public int TotalTickets { get; set; }
}
