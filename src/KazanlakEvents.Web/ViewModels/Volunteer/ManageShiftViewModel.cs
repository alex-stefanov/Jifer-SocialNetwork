namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class ManageShiftViewModel
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxVolunteers { get; set; }
    public int SignedUpCount { get; set; }
    public IReadOnlyList<ManageSignupViewModel> Signups { get; set; } = [];
    public bool IsFull => SignedUpCount >= MaxVolunteers;
}
