using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KazanlakEvents.Web.ViewModels.Blog;

public class BlogPostCreateViewModel
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    public int? CategoryId { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
}
