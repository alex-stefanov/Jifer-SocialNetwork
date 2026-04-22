using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Event;

public class MyEventViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public EventStatus Status { get; set; }
    public string? CoverImageUrl { get; set; }

    public int TicketsSold { get; set; }
    public int AttendeeCount { get; set; }
    public int VolunteerCount { get; set; }
    public bool HasVolunteerTasks { get; set; }

    public Guid OrganizerId { get; set; }
}
