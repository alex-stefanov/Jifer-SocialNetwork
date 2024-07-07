namespace Jifer.Data.Models
{
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Representation of an invitation between users
    /// </summary>
    [Comment("JInvitation Class")]
    public class JInvitation
    {
        /// <summary>
        /// Initializes an instance of the JInvitation class.
        /// </summary>
        /// <param name="sender">The user who sent the invitation.</param>
        /// <param name="receiver">The user who received the invitation.</param>
        /// <param name="expirationDate">The expiration date of the invitation.</param>
        public JInvitation(JUser sender, JUser receiver, DateTime expirationDate)
        { 
            this.Sender = sender;
            this.Receiver = receiver;
            this.CreationDate = DateTime.Now;
            this.ExpirationDate = expirationDate;
            this.InvitationCode = new Guid();
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
        [Comment("Creation Date")]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Sender of the invitation.
        /// </summary>
        [Required]
        [ForeignKey("SenderId")]
        [Comment("User who sent the invitation")]
        public JUser Sender { get; set; } = null!;

        /// <summary>
        /// Receiver of the invitation.
        /// </summary>
        [Required]
        [ForeignKey("ReceiverId")]
        [Comment("User who was sent the invitation")]
        public JUser Receiver { get; set; } = null!;

        /// <summary>
        /// Date/time of expiration.
        /// </summary>
        [Required]
        [Comment("Expiration date")]
        public DateTime ExpirationDate
        {
            get {  return this.ExpirationDate; }
            private set
            {
                if (value > this.CreationDate)
                {
                    this.ExpirationDate = value;
                }
                else
                {
                    throw new ArgumentException("Expiration date must be in the future.");
                }
            }
        }

        /// <summary>
        /// Unique invitation code.
        /// </summary>
        [Required]
        [Comment("Unique invitation code")]
        public Guid InvitationCode { get; set; }

        /// <summary>
        /// Flag for activity => used in DB
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Updates the JInvitation based on expiration
        /// </summary>
        public void UpdateStatus()
        {
            if (DateTime.Now > ExpirationDate)
            {
                this.IsActive = false;
            }
        }

        //TO DO:
        //Accepting the invite

    }
}
