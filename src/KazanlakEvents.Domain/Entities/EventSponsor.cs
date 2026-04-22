using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KazanlakEvents.Domain.Entities;

[Table("EventSponsors")]
public class EventSponsor
{
    public Guid EventId { get; set; }
    public Guid SponsorId { get; set; }

    [Required]
    public int ImpressionCount { get; set; }

    [Required]
    public int ClickCount { get; set; }

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey(nameof(SponsorId))]
    public virtual Sponsor Sponsor { get; set; } = null!;
}
