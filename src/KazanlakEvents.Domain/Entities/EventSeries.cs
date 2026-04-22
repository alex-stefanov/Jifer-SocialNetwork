using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("EventSeries")]
public class EventSeries : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, MaxLength(500)]
    public string RecurrenceRule { get; set; } = string.Empty;

    [Required]
    public Guid OrganizerId { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
