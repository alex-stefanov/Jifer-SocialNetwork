using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("OrganizedEventSponsors")]
public class OrganizedEventSponsor : BaseEntity
{
    [Required]
    public Guid OrganizedEventId { get; set; }

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(300)]
    public string? WebsiteUrl { get; set; }

    [ForeignKey(nameof(OrganizedEventId))]
    public virtual OrganizedEvent OrganizedEvent { get; set; } = null!;
}
