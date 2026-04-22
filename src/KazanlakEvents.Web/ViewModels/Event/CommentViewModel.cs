namespace KazanlakEvents.Web.ViewModels.Event;

public class CommentViewModel
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsEdited { get; set; }
    public int UpvoteCount { get; set; }
    public Guid? ParentCommentId { get; set; }
    public List<CommentViewModel> Replies { get; set; } = new();
}
