using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Comments")]
[Index(nameof(EventId), nameof(IsHidden), nameof(ParentCommentId), Name = "IX_Comments_EventId_IsHidden_ParentCommentId")]
public class Comment : AuditableEntity
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public Guid? ParentCommentId { get; set; }

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public bool IsEdited { get; set; }

    [Required]
    public bool IsHidden { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int UpvoteCount { get; set; }

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey(nameof(ParentCommentId))]
    public virtual Comment? ParentComment { get; set; }

    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
