namespace KazanlakEvents.Web.ViewModels.Admin;

public class BlogAuthorRequestListViewModel
{
    public List<BlogAuthorRequestItemViewModel> Requests { get; set; } = new();
    public int PendingCount { get; set; }
}

public class BlogAuthorRequestItemViewModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsReviewed { get; set; }
    public bool IsApproved { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByName { get; set; }
}
