using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class SignupStatusApiRequest
{
    /// <summary>New status: Registered, Confirmed, Attended, NoShow, Cancelled</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}
