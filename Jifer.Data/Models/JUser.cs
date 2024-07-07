namespace Jifer.Data.Models
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations;
    using Jifer.Data.Constants;

    /// <summary>
    /// Class for custom user class
    /// </summary>
    [Comment("JInvitation Class")]
    [Index(nameof(UserName), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class JUser : IdentityUser
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public JUser() { }

        /// <summary>
        /// Initializes an instance of the JUser class.
        /// </summary>
        /// <param name="names">An array containing first name, middle name, and last name.</param>
        public JUser(params string[] names)
        {
            if (names.Length > 0) this.FirstName = names[0];
            if (names.Length > 1) this.LastName = names[1];
            if (names.Length > 2) this.MiddleName = names[2];
        }

        /// <summary>
        /// First name of the user.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Comment("User's first name")]
        public string FirstName { get; set; } = null!;

        /// <summary>
        ///  Middle name of the user.
        /// </summary>
        [MaxLength(50)]
        [Comment("User's middle name")]
        public string? MiddleName { get; set; }

        /// <summary>
        ///  Last name of the user.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Comment("User's last name")]
        public string LastName { get; set; } = null!;

        /// <summary>
        /// Accessibility level.
        /// </summary>
        [Required]
        [Comment("Profile accessibility level")]
        public ValidationConstants.Accessibility Accessibility { get; set; } = ValidationConstants.Accessibility.Public;

        /// <summary>
        /// Flag for activity => used in DB
        /// </summary>
        [Required]
        [Comment("Is the user active?")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gender of the user.
        /// </summary>
        [Comment("User's gender")]
        public ValidationConstants.ProfileGender Gender { get; set; }

        /// <summary>
        /// Date of birth of the user.
        /// </summary>
        [DataType(DataType.Date)]
        [Comment("User's date of birth")]
        public DateTime DateOfBirth { get; set; }

    }
}