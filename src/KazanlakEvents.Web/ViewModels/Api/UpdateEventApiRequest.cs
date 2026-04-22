using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class UpdateEventApiRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Guid? VenueId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(1, 100_000)]
    public int? Capacity { get; set; }

    [Range(0, 100)]
    public int? MinAge { get; set; }

    public bool IsFree { get; set; }
    public bool IsAccessible { get; set; }
    public List<int>? TagIds { get; set; }
}
