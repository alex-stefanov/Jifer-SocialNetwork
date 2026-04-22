using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("TeamMembers")]
public class TeamMember : AuditableEntity
{
    [Required]
    public Guid OrganizedEventId { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Quote { get; set; }

    [Required, MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Tags { get; set; }

    [MaxLength(200)]
    public string? InstagramUrl { get; set; }

    [MaxLength(200)]
    public string? EmailAddress { get; set; }

    public Guid? LinkedUserId { get; set; }

    public int SortOrder { get; set; }

    [ForeignKey(nameof(OrganizedEventId))]
    public virtual OrganizedEvent OrganizedEvent { get; set; } = null!;
}
