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
        [DataType(DataType.Date)]
        [Comment("Date and time of sending")]
        public DateTime SendDate { get; set; }

        /// <summary>
        /// Id of the sender of the JShip.
        /// </summary>
        [Required]
        [Comment("Id of the user who sent the JShip")]
        public string SenderId { get; set; }

        /// <summary>
        /// Sender of the JShip.
        /// </summary>
        [Required]
        [ForeignKey(nameof(SenderId))]
        [Comment("User who sent the JShip")]
        public virtual JUser Sender { get; set; } = null!;


        /// <summary>
        /// Id of the receiver of the JShip.
        /// </summary>
        [Required]
        [Comment("Id of the user who was sent the JShip")]
        public string ReceiverId { get; set; }

        /// <summary>
        /// Receiver of the JShip.
        /// </summary>
        [Required]
        [ForeignKey(nameof(ReceiverId))]
        [Comment("User who was sent the JShip")]
        public virtual JUser Receiver { get; set; } = null!;

        /// <summary>
        /// Date/time of interacting with the JShip.
        /// </summary>
        [DataType(DataType.Date)]
        [Comment("Date and time of interaction")]
        public DateTime? InteractionDate { get; set; }

        /// <summary>
        /// Date/time of withdraw.
        /// </summary>
        [DataType(DataType.Date)]
        [Comment("Date and time of withdrawal")]
        public DateTime? WithdrawnDate { get; set; }

        /// <summary>
        /// Id of the user who withdrew the JShip.
        /// </summary>
        [Comment("Id of the user who withdrew the JShip")]
        public string? WithdrawnById { get; set; }

        /// <summary>
        /// Withdrawer of the JShip.
        /// </summary>
        [ForeignKey(nameof(WithdrawnById))]
        [Comment("User who withdrew the JShip")]
        public virtual JUser? WithdrawnBy { get; set; }

        /// <summary>
        /// Status of the JShip
        /// </summary>
        [Required]
        [Comment("JShip status")]
        public ValidationConstants.FriendshipStatus Status { get; set; }

        /// <summary>
        /// Flag for activity => used in DB
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;
    }
}
