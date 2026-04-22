using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Admin;

public class TicketListViewModel
{
    public List<TicketAdminItem> Tickets { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? EventFilter { get; set; }
    public string? UserFilter { get; set; }
    public string? StatusFilter { get; set; }
    public List<EventFilterOption> EventOptions { get; set; } = new();
}

public class TicketAdminItem
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string? HolderName { get; set; }
    public string? HolderEmail { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public string QrCode { get; set; } = string.Empty;
}

public class EventFilterOption
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
