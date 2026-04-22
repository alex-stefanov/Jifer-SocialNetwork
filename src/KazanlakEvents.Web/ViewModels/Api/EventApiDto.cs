namespace KazanlakEvents.Web.ViewModels.Api;

public class EventApiDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? VenueName { get; set; }
    public string? VenueCity { get; set; }
    public bool IsFree { get; set; }
    public decimal? LowestPrice { get; set; }
    public int AttendeeCount { get; set; }
    public double AverageRating { get; set; }
}
