using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class ApiLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
