using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class UpdateProfileApiRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Bio { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
