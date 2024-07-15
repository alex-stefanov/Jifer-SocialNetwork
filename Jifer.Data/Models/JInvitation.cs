namespace Jifer.Data.Models
{
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using Jifer.Data.Constants;

    /// <summary>
    /// Representation of an invitation between users
    /// </summary>
    [Comment("JInvitation Class")]
    public class JInvitation
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public JInvitation() { }

        /// <summary>
        /// Initializes an instance of the JInvitation class.
        /// </summary>
        /// <param name="sender">The user who sent the invitation.</param>
        /// <param name="inviteeemial">The email of the user who received the invitation.</param>
        /// <param name="expirationDate">The expiration date of the invitation.</param>
        public JInvitation(JUser sender, string inviteeemial, DateTime expirationDate)
        { 
            this.Sender = sender;
            this.InviteeEmail = inviteeemial;
            this.CreationDate = DateTime.Now;
            this.ExpirationDate = expirationDate;
            this.Sender.SentJInvitations.Add(this);
        }

        /// <summary>
        /// JInvitation Id => Primary Key
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Comment("JInvitation identificator")]
        public int Id { get; set; }

        /// <summary>
        /// Date/time of making the invite.
        /// </summary>
        [Required]
        [DataType(DataType.Date)]
        [Comment("Creation Date")]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Id of the sender of the invitation.
        /// </summary>
        [Required]
        [Comment("Id of the user who sent the invitation")]
        public string SenderId { get; set; }

        /// <summary>
        /// Sender of the invitation.
        /// </summary>
        [Required]
        [ForeignKey("SenderId")]
        [Comment("User who sent the invitation")]
        public virtual JUser Sender { get; set; } = null!;

        /// <summary>
        /// Email of the invitee.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(ValidationConstants.EmailsMaxLength)]
        [Comment("Email of the invited user")]
        public string InviteeEmail { get; set; } = null!;

        /// <summary>
        /// Date/time of expiration.
        /// </summary>
        [Required]
        [DataType(DataType.Date)]
        [Comment("Expiration date")]
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Unique invitation code.
        /// </summary>
        [Required]
        [Comment("Unique invitation code")]
        public Guid InvitationCode { get; set; }

        /// <summary>
        /// Flag for activity => used in DB
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Checks if the invite has expired.
        /// </summary>
        public bool IsExpired()
        {
            if (DateTime.Now > this.ExpirationDate)
            {
                this.IsActive = false;
                return true;
            }
            return false;
        }
    }
}
