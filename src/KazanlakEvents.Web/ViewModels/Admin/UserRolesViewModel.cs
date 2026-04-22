namespace KazanlakEvents.Web.ViewModels.Admin;

public class UserRolesViewModel
{
    public string? SearchQuery { get; set; }
    public List<UserRoleItem> Users { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
}

public class UserRoleItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Initials { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
