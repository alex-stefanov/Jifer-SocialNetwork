namespace KazanlakEvents.Web.ViewModels.Api;

public class BlogPostDetailApiDto : BlogPostApiDto
{
    public string Content { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
}
