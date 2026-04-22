using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("VolunteerTasks")]
public class VolunteerTask : BaseEntity
{
    [Required]
    public Guid EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int VolunteersNeeded { get; set; }

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<VolunteerShift> Shifts { get; set; } = new List<VolunteerShift>();
}
