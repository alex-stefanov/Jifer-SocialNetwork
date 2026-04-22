using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Entities;

[Table("Reports")]
public class Report : BaseEntity
{
    [Required]
    public Guid ReporterId { get; set; }

    [Required]
    public ReportTargetType TargetType { get; set; }

    [Required]
    public Guid TargetId { get; set; }

    [Required]
    public ReportReason Reason { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public Guid? ReviewedById { get; set; }

    [MaxLength(500)]
    public string? ReviewNotes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
}
