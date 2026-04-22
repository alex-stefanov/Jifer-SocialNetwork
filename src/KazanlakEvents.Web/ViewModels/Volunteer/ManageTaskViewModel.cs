namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class ManageTaskViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VolunteersNeeded { get; set; }
    public IReadOnlyList<ManageShiftViewModel> Shifts { get; set; } = [];
}
