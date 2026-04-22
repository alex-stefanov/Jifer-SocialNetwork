using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KazanlakEvents.Domain.Entities;

[Table("VenueRequests")]
public class VenueRequest : AuditableEntity
{
    [Required]
    public Guid RequestedByUserId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Address { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public VenueRequestStatus Status { get; set; } = VenueRequestStatus.Pending;

    public Guid? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? ReviewNotes { get; set; }

    public Guid? CreatedVenueId { get; set; }
}
