namespace KazanlakEvents.Web.ViewModels.Api;

public class ApiLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public ApiUserInfo User { get; set; } = null!;
}
