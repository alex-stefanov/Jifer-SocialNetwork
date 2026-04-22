using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Tickets")]
[Index(nameof(TicketNumber), IsUnique = true)]
[Index(nameof(QrCode), IsUnique = true)]
[Index(nameof(TicketTypeId), nameof(Status), Name = "IX_Tickets_TicketTypeId_Status")]
public class Ticket : BaseEntity
{
    [Required, MaxLength(30)]
    public string TicketNumber { get; set; } = string.Empty;

    [Required]
    public Guid OrderItemId { get; set; }

    [Required]
    public Guid TicketTypeId { get; set; }

    public Guid? HolderId { get; set; }

    [MaxLength(256)]
    public string? HolderEmail { get; set; }

    [Required, MaxLength(500)]
    public string QrCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? QrCodeImageUrl { get; set; }

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Valid;

    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInById { get; set; }

    [Required]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TicketTypeId))]
    public virtual TicketType TicketType { get; set; } = null!;
}
