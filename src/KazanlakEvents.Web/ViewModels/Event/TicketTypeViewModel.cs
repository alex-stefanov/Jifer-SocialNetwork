namespace KazanlakEvents.Web.ViewModels.Event;

public class TicketTypeViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public int AvailableQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public int MaxPerOrder { get; set; }
}
