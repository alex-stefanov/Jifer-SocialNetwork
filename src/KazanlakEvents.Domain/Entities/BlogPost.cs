using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Domain.Entities;

[Table("BlogPosts")]
public class BlogPost : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [Required]
    public Guid AuthorId { get; set; }

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual BlogCategory? Category { get; set; }

    public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;

    [NotMapped]
    public bool IsPublished => Status == BlogPostStatus.Published;

    [Required]
    public bool IsFeatured { get; set; }

    public DateTime? PublishedAt { get; set; }

    [Range(0, int.MaxValue)]
    public int ViewCount { get; set; }

    public virtual ICollection<BlogPostTag> Tags { get; set; } = new List<BlogPostTag>();
}
