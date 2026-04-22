namespace KazanlakEvents.Web.ViewModels.Api;

public class TicketTypeApiDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public bool IsAvailable { get; set; }
}
