namespace KazanlakEvents.Web.ViewModels.Api;

public class UserProfileApiDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? City { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int OrganizedEventCount { get; set; }
    public int AttendedEventCount { get; set; }
}
