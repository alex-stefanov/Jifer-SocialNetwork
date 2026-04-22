using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Admin;

public class VenueRequestItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Notes { get; set; }
    public VenueRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}
