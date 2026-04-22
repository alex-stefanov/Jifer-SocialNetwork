using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Events")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(StartDate))]
[Index(nameof(Status))]
[Index(nameof(CategoryId))]
[Index(nameof(OrganizerId))]
[Index(nameof(IsFree))]
[Index(nameof(Status), nameof(StartDate), Name = "IX_Events_Status_StartDate")]
[Index(nameof(CategoryId), nameof(Status), nameof(StartDate), Name = "IX_Events_CategoryId_Status_StartDate")]
public class Event : AuditableEntity, ISoftDeletable
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [Required]
    public Guid OrganizerId { get; set; }

    public Guid? VenueId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Guid? SeriesId { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0, int.MaxValue)]
    public int? Capacity { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Required]
    public bool IsFree { get; set; }

    [Required]
    public bool IsAccessible { get; set; }

    [Required]
    public EventStatus Status { get; set; } = EventStatus.Draft;

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    [Required]
    public int ViewCount { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    [MaxLength(100)]
    public string? DeletedBy { get; set; }

    [ForeignKey(nameof(VenueId))]
    public virtual Venue? Venue { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;

    [ForeignKey(nameof(SeriesId))]
    public virtual EventSeries? Series { get; set; }

    public virtual ICollection<EventTag> EventTags { get; set; } = new List<EventTag>();
    public virtual ICollection<EventImage> Images { get; set; } = new List<EventImage>();
    public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<EventAttendance> Attendances { get; set; } = new List<EventAttendance>();
    public virtual ICollection<VolunteerTask> VolunteerTasks { get; set; } = new List<VolunteerTask>();
    public virtual ICollection<EventSponsor> Sponsors { get; set; } = new List<EventSponsor>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
