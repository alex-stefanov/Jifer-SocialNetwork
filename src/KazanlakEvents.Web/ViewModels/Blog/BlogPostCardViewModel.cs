using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Blog;

public class BlogPostCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public int? CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public BlogPostStatus Status { get; set; }
    public bool IsPublished => Status == BlogPostStatus.Published;
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public string? AuthorName { get; set; }
    public int ReadTimeMinutes { get; set; }
}
