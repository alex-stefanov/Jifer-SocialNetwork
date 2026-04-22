using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Ratings")]
[Index(nameof(EventId), nameof(UserId), IsUnique = true)]
public class Rating : BaseEntity
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required, Range(1, 5)]
    public int Score { get; set; }

    [MaxLength(1000)]
    public string? ReviewText { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;
}
