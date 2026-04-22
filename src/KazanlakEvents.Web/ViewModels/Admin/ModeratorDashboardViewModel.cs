using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Admin;

public class ModeratorDashboardViewModel
{
    public int PendingEventsCount { get; set; }
    public int RecentlyModifiedEventsCount { get; set; }
    public int PendingBlogPostsCount { get; set; }
    public int PendingAuthorRequestsCount { get; set; }

    public List<PendingEventItem> PendingEvents { get; set; } = new();
    public List<PendingEventItem> RecentlyModifiedEvents { get; set; } = new();
    public List<PendingBlogPostItem> PendingBlogPosts { get; set; } = new();
}

public class PendingEventItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? OrganizerName { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? CategoryName { get; set; }
    public EventStatus Status { get; set; }
}

public class PendingBlogPostItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? AuthorName { get; set; }
    public DateTime SubmittedAt { get; set; }
}
