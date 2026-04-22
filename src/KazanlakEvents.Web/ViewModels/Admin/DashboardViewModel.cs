using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Admin;

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveEvents { get; set; }
    public int TotalRegistrations { get; set; }
    public int PendingApprovals { get; set; }

    public int UsersCurrentMonth { get; set; }
    public int UsersPrevMonth { get; set; }
    public int ActiveEventsCurrentMonth { get; set; }
    public int ActiveEventsPrevMonth { get; set; }
    public int TicketsCurrentMonth { get; set; }
    public int TicketsPrevMonth { get; set; }

    public List<string> MonthLabels { get; set; } = new();
    public List<int> EventsPerMonth { get; set; } = new();

    public List<RecentEventItem> RecentEvents { get; set; } = new();
}

public class TopItemViewModel
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class RecentEventItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public EventStatus Status { get; set; }
}
