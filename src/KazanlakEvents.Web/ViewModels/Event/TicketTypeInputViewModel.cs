namespace KazanlakEvents.Web.ViewModels.Event;

public class TicketTypeInputViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 100;
    public string? Description { get; set; }
}
