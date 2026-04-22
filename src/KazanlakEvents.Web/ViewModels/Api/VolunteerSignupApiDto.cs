namespace KazanlakEvents.Web.ViewModels.Api;

public class VolunteerSignupApiDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? HoursLogged { get; set; }
    public DateTime SignedUpAt { get; set; }
}
