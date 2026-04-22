using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("EventReferrals")]
public class EventReferral : BaseEntity
{
    [Required]
    public Guid EventId { get; set; }

    public Guid? ReferrerUserId { get; set; }

    [Required, MaxLength(50)]
    public string ReferralCode { get; set; } = string.Empty;

    public int ClickCount { get; set; }

    public int ConversionCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
