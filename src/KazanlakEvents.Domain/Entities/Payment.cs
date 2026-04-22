using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Entities;

[Table("Payments")]
public class Payment : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }

    [MaxLength(200)]
    public string? StripePaymentIntentId { get; set; }

    [MaxLength(200)]
    public string? StripeChargeId { get; set; }

    [Required, Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [MaxLength(50)]
    public string? Method { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RefundedAmount { get; set; }

    public DateTime? PaidAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(OrderId))]
    public virtual Order Order { get; set; } = null!;
}
