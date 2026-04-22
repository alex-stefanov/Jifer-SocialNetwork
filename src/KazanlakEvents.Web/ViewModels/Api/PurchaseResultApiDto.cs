namespace KazanlakEvents.Web.ViewModels.Api;

public class PurchaseResultApiDto
{
    public string? OrderNumber { get; set; }
    public List<TicketApiDto> Tickets { get; set; } = new();
}
