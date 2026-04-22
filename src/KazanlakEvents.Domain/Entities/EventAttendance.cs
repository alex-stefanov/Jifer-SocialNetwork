using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("EventAttendances")]
[Index(nameof(EventId), Name = "IX_EventAttendances_EventId")]
public class EventAttendance
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid EventId { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;
}
