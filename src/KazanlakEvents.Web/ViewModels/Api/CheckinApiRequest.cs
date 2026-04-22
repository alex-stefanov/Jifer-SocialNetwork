using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class CheckinApiRequest
{
    [Required]
    public string QrCode { get; set; } = string.Empty;
}
