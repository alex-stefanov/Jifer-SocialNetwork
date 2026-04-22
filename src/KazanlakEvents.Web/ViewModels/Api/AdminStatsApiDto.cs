namespace KazanlakEvents.Web.ViewModels.Api;

public class AdminStatsApiDto
{
    public int TotalUsers { get; set; }
    public int TotalEvents { get; set; }
    public int PendingApprovals { get; set; }
    public int TotalTicketsSold { get; set; }
}
