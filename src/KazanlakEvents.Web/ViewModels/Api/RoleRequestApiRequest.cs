using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class RoleRequestApiRequest
{
    /// <summary>Requested role: "Organizer" or "BlogAuthor"</summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
