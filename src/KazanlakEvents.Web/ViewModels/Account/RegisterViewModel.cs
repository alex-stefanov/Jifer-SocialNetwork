using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "FieldRequired")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "MaxLengthExceeded")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "FieldRequired")]
    [EmailAddress(ErrorMessage = "InvalidEmailFormat")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "FieldRequired")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "MaxLengthExceeded")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "FieldRequired")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "MaxLengthExceeded")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "FieldRequired")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "FieldLengthRange")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "PasswordMismatch")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
}
