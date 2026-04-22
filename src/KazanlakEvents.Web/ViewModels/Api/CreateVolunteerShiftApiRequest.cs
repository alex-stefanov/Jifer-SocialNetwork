using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class CreateVolunteerShiftApiRequest
{
    [Required]
    public Guid TaskId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int MaxVolunteers { get; set; }
}
