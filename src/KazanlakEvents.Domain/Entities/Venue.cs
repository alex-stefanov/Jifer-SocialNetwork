using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("Venues")]
public class Venue : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, Column(TypeName = "decimal(9,6)")]
    public decimal Latitude { get; set; }

    [Required, Column(TypeName = "decimal(9,6)")]
    public decimal Longitude { get; set; }

    [Range(0, int.MaxValue)]
    public int? Capacity { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    public Guid CreatedByUserId { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
