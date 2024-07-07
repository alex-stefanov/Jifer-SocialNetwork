namespace Jifer.Data.Models
{
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using Jifer.Data.Constants;

    /// <summary>
    /// Representation of a friendship between users.
    /// </summary>
    [Comment("JShip Class")]
    public class JShip
    {
        /// <summary>
        /// Initializes an instance of the JShip class.
        /// </summary>
        /// <param name="sender">The user who sent the friendship request.</param>
        /// <param name="receiver">The user who received the friendship request.</param>
        public JShip(JUser sender, JUser receiver)
        {
            this.Sender = sender;
            this.Receiver = receiver;
            this.SendDate = DateTime.Now;
            this.Status = ValidationConstants.FriendshipStatus.Pending;
        }

        /// <summary>
        /// JShip Id => Primary Key
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Comment("JShip identificator")]
        public int Id { get; set; }

        /// <summary>
        /// Date/time of sending the JShip.
        /// </summary>
        [Required]
        [Comment("Date and time of sending")]
        public DateTime SendDate { get; set; }

        /// <summary>
        /// Sender of the JShip.
        /// </summary>
        [Required]
        [ForeignKey("SenderId")]
        [Comment("User who sent the JShip")]
        public JUser Sender { get; set; } = null!;

        /// <summary>
        /// Receiver of the JShip.
        /// </summary>
        [Required]
        [ForeignKey("ReceiverId")]
        [Comment("User who was sent the JShip")]
        public JUser Receiver { get; set; } = null!;

        /// <summary>
        /// Date/time of interacting with the JShip.
        /// </summary>
        [Comment("Date and time of interaction")]
        public DateTime? InteractionDate { get; set; }

        /// <summary>
        /// Date/time of withdraw.
        /// </summary>
        [Comment("Date and time of withdrawal")]
        public DateTime? WithdrawnDate { get; set; }

        /// <summary>
        /// Withdrawer of the JShip.
        /// </summary>
        [Comment("User who withdrew the JShip")]
        public JUser? WithdrawnBy { get; set; }

        /// <summary>
        /// Status of the JShip
        /// </summary>
        [Required]
        [Comment("JShip status")]
        public ValidationConstants.FriendshipStatus Status { get; set; }

        /// <summary>
        /// Accepts the friendship request, setting the status and interaction date to confirmed and now (respectfully).Returns true/false whether it is possible.
        /// </summary>
        public bool Accept()
        {
            if (this.Status != ValidationConstants.FriendshipStatus.Pending) return false;

            this.InteractionDate = DateTime.Now;
            this.Status = ValidationConstants.FriendshipStatus.Confirmed;

            return true;
        }

        /// <summary>
        /// Accepts the friendship request, setting the status and interaction date to rejected and now (respectfully).Returns true/false whether it is possible.
        /// </summary>
        public bool Reject()
        {
            if (this.Status != ValidationConstants.FriendshipStatus.Pending) return false;

            this.InteractionDate = DateTime.Now;
            this.Status = ValidationConstants.FriendshipStatus.Rejected;

            return true;
        }

        /// <summary>
        /// Withdraws the friendship request, setting the status and withdrawal date to withdrawn and now (respectfully).Returns true/false whether it is possible.
        /// </summary>
        /// <param name="user">The user withdrawing the friendship request.</param>
        public bool Withdraw(JUser user)
        {
            if (this.Status != ValidationConstants.FriendshipStatus.Pending) return false;

            this.WithdrawnDate = DateTime.Now;
            this.WithdrawnBy = user;
            this.Status = ValidationConstants.FriendshipStatus.Withdrawn;

            return true;
        }
    }
}
