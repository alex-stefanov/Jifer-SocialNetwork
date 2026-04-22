using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Profile;

public class TicketViewModel
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventSlug { get; set; } = string.Empty;
    public string? EventCoverImageUrl { get; set; }
    public DateTime EventStartDate { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public string? QrCodeImageUrl { get; set; }
}
