using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    [MaxLength(100)]
    public string? ModifiedBy { get; set; }
}
