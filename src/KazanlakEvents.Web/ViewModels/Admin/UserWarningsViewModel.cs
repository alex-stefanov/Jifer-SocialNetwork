namespace KazanlakEvents.Web.ViewModels.Admin;

public class UserWarningsViewModel
{
    public string? SearchUsername { get; set; }
    public Guid? UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public IReadOnlyList<UserWarningViewModel> Warnings { get; set; } = [];
    public bool IsBanned { get; set; }
}
