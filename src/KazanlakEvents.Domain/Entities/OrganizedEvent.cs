using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("OrganizedEvents")]
[Index(nameof(Slug), IsUnique = true)]
public class OrganizedEvent : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    public DateTime? EventDate { get; set; }

    public int? AttendeesCount { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

    public virtual ICollection<OrganizedEventSponsor> Sponsors { get; set; } = new List<OrganizedEventSponsor>();
}
