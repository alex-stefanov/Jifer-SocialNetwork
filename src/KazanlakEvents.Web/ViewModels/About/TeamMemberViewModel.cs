namespace KazanlakEvents.Web.ViewModels.About;

public class TeamMemberViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Description { get; set; }
    public string? Quote { get; set; }
    public string Role { get; set; } = string.Empty;
    public string[] TagList { get; set; } = [];
    public string? InstagramUrl { get; set; }
    public string? EmailAddress { get; set; }
    public Guid? LinkedUserId { get; set; }
    public int SortOrder { get; set; }
}
