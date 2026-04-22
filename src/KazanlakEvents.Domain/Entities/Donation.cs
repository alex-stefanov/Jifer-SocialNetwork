using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("Donations")]
public class Donation : BaseEntity
{
    [Required]
    public Guid CampaignId { get; set; }

    public Guid? DonorId { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? StripePaymentIntentId { get; set; }

    [Required]
    public bool IsAnonymous { get; set; }

    [Required]
    public bool IsRecurring { get; set; }

    [MaxLength(200)]
    public string? StripeSubscriptionId { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    [Required]
    public DateTime DonatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CampaignId))]
    public virtual DonationCampaign Campaign { get; set; } = null!;
}
