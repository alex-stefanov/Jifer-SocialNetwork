namespace KazanlakEvents.Web.ViewModels.Api;

public class VolunteerShiftApiDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxVolunteers { get; set; }
    public int SignedUpCount { get; set; }
}
