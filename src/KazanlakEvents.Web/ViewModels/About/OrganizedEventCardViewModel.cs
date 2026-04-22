namespace KazanlakEvents.Web.ViewModels.About;

public class OrganizedEventCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public DateTime? EventDate { get; set; }
    public int? AttendeesCount { get; set; }
    public string DescriptionExcerpt { get; set; } = string.Empty;
}
