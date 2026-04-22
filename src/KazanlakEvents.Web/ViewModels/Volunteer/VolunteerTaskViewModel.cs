namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class VolunteerTaskViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VolunteersNeeded { get; set; }
    public IReadOnlyList<VolunteerShiftViewModel> Shifts { get; set; } = [];
}
