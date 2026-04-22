namespace KazanlakEvents.Web.ViewModels.Api;

public class EventApiDetailDto
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

    public string Description { get; set; } = string.Empty;
    public string? VenueAddress { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsAccessible { get; set; }
    public int? Capacity { get; set; }
    public int? MinAge { get; set; }
    public int ViewCount { get; set; }
    public string OrganizerUserName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<TicketTypeApiDto> TicketTypes { get; set; } = new();
}
