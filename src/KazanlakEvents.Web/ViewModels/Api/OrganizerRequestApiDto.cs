namespace KazanlakEvents.Web.ViewModels.Api;

public class OrganizerRequestApiDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsReviewed { get; set; }
    public DateTime CreatedAt { get; set; }
}
