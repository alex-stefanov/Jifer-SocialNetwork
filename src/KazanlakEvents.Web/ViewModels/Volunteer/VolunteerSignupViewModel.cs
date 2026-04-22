using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class VolunteerSignupViewModel
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventSlug { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }
    public VolunteerSignupStatus Status { get; set; }
    public decimal? HoursLogged { get; set; }
    public DateTime SignedUpAt { get; set; }
}
