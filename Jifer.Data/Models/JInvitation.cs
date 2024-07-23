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
        [ForeignKey(nameof(SenderId))]
        [Comment("User who sent the invitation")]
        public virtual JUser Sender { get; set; } = null!;

        public string? ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public virtual JUser? Receiver { get; set; }

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
    }
}
