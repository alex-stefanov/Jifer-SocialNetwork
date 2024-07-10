using System.ComponentModel.DataAnnotations;
using Jifer.Data.Constants;
using static Jifer.Data.Constants.ValidationConstants;

namespace Jifer.Models.Register
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(ValidationConstants.JUserFirstNameMaxLength, MinimumLength = ValidationConstants.JUserFirstNameMinLength)]
        public string FirstName { get; set; } = null!;

        [StringLength(ValidationConstants.JUserMiddleNameMaxLength, MinimumLength = ValidationConstants.JUserMiddleNameMinLength)]
        public string? MiddleName { get; set; }

        [Required]
        [StringLength(ValidationConstants.JUserLastNameMaxLength, MinimumLength = ValidationConstants.JUserLastNameMinLength)]
        public string LastName { get; set; } = null!;

        [Required]
        [StringLength(ValidationConstants.JUserUsernameMaxLength, MinimumLength = ValidationConstants.JUserUsernameMinLength)]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(ValidationConstants.EmailsMaxLength, MinimumLength = ValidationConstants.EmailsMinLength)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = ValidationConstants.PasswordMinLength)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Compare(nameof(Password))]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;

        [Required]
        public ValidationConstants.Accessibility Accessibility { get; set; }

        public ValidationConstants.ProfileGender? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string InviteCode { get; set; }=null!;

    }
}
