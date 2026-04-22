using System.ComponentModel.DataAnnotations.Schema;

namespace KazanlakEvents.Domain.Entities;

[Table("EventTags")]
public class EventTag
{
    public Guid EventId { get; set; }
    public int TagId { get; set; }

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey(nameof(TagId))]
    public virtual Tag Tag { get; set; } = null!;
}
