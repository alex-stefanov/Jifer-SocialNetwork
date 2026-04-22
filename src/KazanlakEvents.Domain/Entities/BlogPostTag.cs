using System.ComponentModel.DataAnnotations.Schema;

namespace KazanlakEvents.Domain.Entities;

[Table("BlogPostTags")]
public class BlogPostTag
{
    public Guid BlogPostId { get; set; }
    public int TagId { get; set; }

    [ForeignKey(nameof(BlogPostId))]
    public virtual BlogPost BlogPost { get; set; } = null!;

    [ForeignKey(nameof(TagId))]
    public virtual Tag Tag { get; set; } = null!;
}
