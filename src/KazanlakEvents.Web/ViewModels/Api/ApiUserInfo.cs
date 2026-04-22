namespace KazanlakEvents.Web.ViewModels.Api;

public class ApiUserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}
