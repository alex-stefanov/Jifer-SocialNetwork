using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Event;

public class EventDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? VenueName { get; set; }
    public string? VenueAddress { get; set; }
    public string? VenueCity { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public bool IsFree { get; set; }
    public bool IsAccessible { get; set; }
    public int? MinAge { get; set; }
    public int? Capacity { get; set; }
    public int ViewCount { get; set; }
    public EventStatus Status { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public Guid OrganizerId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public string? OrganizerAvatarUrl { get; set; }

    public IReadOnlyList<string> Tags { get; set; } = new List<string>();
    public IReadOnlyList<EventImageViewModel> Images { get; set; } = new List<EventImageViewModel>();
    public IReadOnlyList<TicketTypeViewModel> TicketTypes { get; set; } = new List<TicketTypeViewModel>();
    public IReadOnlyList<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
    public IReadOnlyList<ViewModels.Sponsor.SponsorViewModel> Sponsors { get; set; } = new List<ViewModels.Sponsor.SponsorViewModel>();

    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int AttendeeCount { get; set; }

    public Guid? SeriesId { get; set; }
    public string? SeriesTitle { get; set; }

    // Set by controller after AutoMapper — not available at mapping time
    public bool IsOrganizer { get; set; }
    public bool IsFavorited { get; set; }
    public AttendanceStatus? UserAttendance { get; set; }
    public int? CurrentUserRating { get; set; }
    public bool HasVolunteerTasks { get; set; }
    public int UserCurrentTicketCount { get; set; }
}
