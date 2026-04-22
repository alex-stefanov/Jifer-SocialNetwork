using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("VolunteerShifts")]
public class VolunteerShift : BaseEntity
{
    [Required]
    public Guid TaskId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int MaxVolunteers { get; set; }

    [ForeignKey(nameof(TaskId))]
    public virtual VolunteerTask Task { get; set; } = null!;

    public virtual ICollection<VolunteerSignup> Signups { get; set; } = new List<VolunteerSignup>();
}
