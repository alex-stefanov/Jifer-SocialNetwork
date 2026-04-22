using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.About;

public class OrganizedEventFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public DateTime? EventDate { get; set; }

    [Range(0, int.MaxValue)]
    public int? AttendeesCount { get; set; }

    public bool IsActive { get; set; } = true;
}
