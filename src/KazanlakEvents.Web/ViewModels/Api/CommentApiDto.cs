namespace KazanlakEvents.Web.ViewModels.Api;

public class CommentApiDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public int UpvoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CommentApiDto> Replies { get; set; } = new();
}
