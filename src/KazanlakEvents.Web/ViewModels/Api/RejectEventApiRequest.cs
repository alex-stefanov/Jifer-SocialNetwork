using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class RejectEventApiRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
