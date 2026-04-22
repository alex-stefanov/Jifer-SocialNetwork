namespace KazanlakEvents.Web.ViewModels.Event;

public class EventCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? VenueName { get; set; }
    public string? VenueCity { get; set; }
    public int AttendeeCount { get; set; }
    public double AverageRating { get; set; }
    public int CommentCount { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? OrganizerName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsFavorited { get; set; }
}
