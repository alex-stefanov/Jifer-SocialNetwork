namespace KazanlakEvents.Web.ViewModels.Api;

public class TicketApiDto
{
    public string TicketNumber { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public string? QrCodeImageUrl { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string TicketTypeName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
