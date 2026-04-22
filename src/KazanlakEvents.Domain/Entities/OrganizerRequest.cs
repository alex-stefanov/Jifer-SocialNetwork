using KazanlakEvents.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KazanlakEvents.Domain.Entities;

[Table("OrganizerRequests")]
public class OrganizerRequest : AuditableEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public bool IsReviewed { get; set; }

    public Guid? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? ReviewNotes { get; set; }
}
