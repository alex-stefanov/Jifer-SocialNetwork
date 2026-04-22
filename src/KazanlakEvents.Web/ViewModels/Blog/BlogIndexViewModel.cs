using Microsoft.AspNetCore.Mvc.Rendering;

namespace KazanlakEvents.Web.ViewModels.Blog;

public class BlogIndexViewModel
{
    public IReadOnlyList<BlogPostCardViewModel> Posts { get; set; } = new List<BlogPostCardViewModel>();
    public IReadOnlyList<BlogCategoryViewModel> Categories { get; set; } = new List<BlogCategoryViewModel>();
    public BlogPostCardViewModel? FeaturedPost { get; set; }
    public IReadOnlyList<BlogPostCardViewModel> RecentPosts { get; set; } = new List<BlogPostCardViewModel>();
    public int? CategoryFilter { get; set; }
    public string? StatusFilter { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalPosts { get; set; }
}
