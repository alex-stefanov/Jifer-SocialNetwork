using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class ManageSignupViewModel
{
    public Guid Id { get; set; }
    public string VolunteerName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public VolunteerSignupStatus Status { get; set; }
    public decimal? HoursLogged { get; set; }
}
