namespace KazanlakEvents.Web.ViewModels.Api;

public class VolunteerTaskApiDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VolunteersNeeded { get; set; }
    public List<VolunteerShiftApiDto> Shifts { get; set; } = new();
}
