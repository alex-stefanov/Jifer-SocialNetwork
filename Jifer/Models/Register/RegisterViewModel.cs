using System.ComponentModel.DataAnnotations;
using Jifer.Data.Constants;
using static Jifer.Data.Constants.ValidationConstants;

namespace Jifer.Models.Register
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(ValidationConstants.JUserFirstName, MinimumLength = 2)]
        public string FirstName { get; set; } = null!;

        [StringLength(ValidationConstants.JUserMiddleName, MinimumLength = 3)]
        public string? MiddleName { get; set; }

        [Required]
        [StringLength(ValidationConstants.JUserLastName, MinimumLength = 3)]
        public string LastName { get; set; } = null!;

        [Required]
        [StringLength(ValidationConstants.JUserUsername, MinimumLength = 4)]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(ValidationConstants.EmailsMaxLength, MinimumLength = 4)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = 6)]
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
