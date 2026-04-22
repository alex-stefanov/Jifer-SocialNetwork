namespace KazanlakEvents.Web.ViewModels.Admin;

public class AuditLogItemViewModel
{
    public long Id { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class AuditLogListViewModel
{
    public IReadOnlyList<AuditLogItemViewModel> Logs { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public DateTime? FromFilter { get; set; }
    public DateTime? ToFilter { get; set; }
}
