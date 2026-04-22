using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Entities;

[Table("VolunteerSignups")]
public class VolunteerSignup : BaseEntity
{
    [Required]
    public Guid ShiftId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public VolunteerSignupStatus Status { get; set; } = VolunteerSignupStatus.Registered;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? HoursLogged { get; set; }

    public DateTime? CheckedInAt { get; set; }

    [Required]
    public DateTime SignedUpAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ShiftId))]
    public virtual VolunteerShift Shift { get; set; } = null!;
}
