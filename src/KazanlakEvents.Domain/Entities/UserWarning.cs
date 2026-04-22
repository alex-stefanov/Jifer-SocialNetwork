using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Entities;

[Table("UserWarnings")]
public class UserWarning : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid IssuedById { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public WarningType Type { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
