namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class MyShiftsViewModel
{
    public IReadOnlyList<VolunteerSignupViewModel> Signups { get; set; } = [];
    public int TotalHours { get; set; }
    public int EventCount { get; set; }
}
