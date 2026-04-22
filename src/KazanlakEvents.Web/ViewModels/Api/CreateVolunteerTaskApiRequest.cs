using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class CreateVolunteerTaskApiRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int VolunteersNeeded { get; set; }
}
