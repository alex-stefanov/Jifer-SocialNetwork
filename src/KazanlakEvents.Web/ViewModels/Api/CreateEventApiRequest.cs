using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class CreateEventApiRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Guid? VenueId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? Capacity { get; set; }

    public int? MinAge { get; set; }

    public bool IsFree { get; set; }

    public bool IsAccessible { get; set; }

    public List<int>? TagIds { get; set; }
}
