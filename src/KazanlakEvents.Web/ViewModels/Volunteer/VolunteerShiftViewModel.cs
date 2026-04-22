namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class VolunteerShiftViewModel
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxVolunteers { get; set; }
    public int SignedUpCount { get; set; }
    public bool IsUserSignedUp { get; set; }
    public Guid? UserSignupId { get; set; }

    public int SpotsLeft => MaxVolunteers - SignedUpCount;
    public bool IsFull => SpotsLeft <= 0;
}
