using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Entities;

[Table("Sponsors")]
public class Sponsor : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public SponsorTier Tier { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public virtual ICollection<EventSponsor> EventSponsors { get; set; } = new List<EventSponsor>();
}
