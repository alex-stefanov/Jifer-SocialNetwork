using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("WebhookSubscriptions")]
[Index(nameof(UserId))]
[Index(nameof(UserId), nameof(IsActive))]
public class WebhookSubscription : AuditableEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(500)]
    public string CallbackUrl { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required, MaxLength(500)]
    public string Events { get; set; } = string.Empty;

    public DateTime? LastTriggeredAt { get; set; }

    public int FailureCount { get; set; }
}
