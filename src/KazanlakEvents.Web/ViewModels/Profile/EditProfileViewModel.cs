using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KazanlakEvents.Web.ViewModels.Profile;

public class EditProfileViewModel
{
    [Required, MaxLength(100)]
    [Display(Name = "FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "LastName")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Bio")]
    public string? Bio { get; set; }

    [MaxLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [MaxLength(30)]
    [Phone]
    [Display(Name = "PhoneNumber")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "DateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "PreferredLanguage")]
    public string PreferredLanguage { get; set; } = "bg";

    public string? ExistingAvatarUrl { get; set; }

    [Display(Name = "UploadAvatar")]
    public IFormFile? Avatar { get; set; }
}
