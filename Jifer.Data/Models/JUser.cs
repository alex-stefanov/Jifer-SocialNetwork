namespace Jifer.Data.Models
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations;
    using Jifer.Data.Constants;
    using System.ComponentModel.DataAnnotations.Schema;

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
        [MaxLength(ValidationConstants.JUserFirstNameMaxLength)]
        [Comment("User's first name")]
        public string FirstName { get; set; } = null!;

        /// <summary>
        ///  Middle name of the user.
        /// </summary>
        [MaxLength(ValidationConstants.JUserMiddleNameMaxLength)]
        [Comment("User's middle name")]
        public string? MiddleName { get; set; }

        /// <summary>
        ///  Last name of the user.
        /// </summary>
        [Required]
        [MaxLength(ValidationConstants.JUserLastNameMaxLength)]
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
        public ValidationConstants.ProfileGender? Gender { get; set; }

        /// <summary>
        /// Date of birth of the user.
        /// </summary>
        [DataType(DataType.Date)]
        [Comment("User's date of birth")]
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Collection of user's posts.
        /// </summary>
        [Comment("List of JGos")]
        public virtual List<JGo> JGos { get; set; } = new List<JGo>();

        /// <summary>
        /// Collection of user's sent friend requests.
        /// </summary>
        [InverseProperty("Sender")]
        [Comment("List of sent JShip requests")]
        public virtual List<JShip> SentFriendRequests { get; set; } = new List<JShip>();

        /// <summary>
        /// Collection of user's received friend requests.
        /// </summary>
        [InverseProperty("Receiver")]
        [Comment("List of received JShip requests")]
        public virtual List<JShip> ReceivedFriendRequests { get; set; } = new List<JShip>();

        /// <summary>
        /// Collection of user's sent invitations.
        /// </summary>
        [Comment("List of sent JInvitations")]
        public virtual List<JInvitation> SentJInvitations { get; set; } = new List<JInvitation>();
    }
}