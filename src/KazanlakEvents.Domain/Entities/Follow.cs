using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Follows")]
[Index(nameof(FolloweeId), Name = "IX_Follows_FolloweeId")]
public class Follow
{
    [Required]
    public Guid FollowerId { get; set; }

    [Required]
    public Guid FolloweeId { get; set; }

    [Required]
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}
