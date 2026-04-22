using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("EventImages")]
public class EventImage : BaseEntity
{
    [Required]
    public Guid EventId { get; set; }

    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(200)]
    public string? Caption { get; set; }

    [Required]
    public int SortOrder { get; set; }

    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;
}
