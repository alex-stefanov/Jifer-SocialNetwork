using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Favorites")]
[Index(nameof(UserId), Name = "IX_Favorites_UserId")]
public class Favorite
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid EventId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;
}
