namespace KazanlakEvents.Web.ViewModels.Blog;

public class BlogPostDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public int? CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public string? AuthorName { get; set; }
    public int ReadTimeMinutes { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = new List<string>();
    public IReadOnlyList<BlogPostCardViewModel> RelatedPosts { get; set; } = new List<BlogPostCardViewModel>();
}
