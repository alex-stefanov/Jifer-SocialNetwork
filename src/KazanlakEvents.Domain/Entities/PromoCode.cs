using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("PromoCodes")]
[Index(nameof(Code), IsUnique = true)]
public class PromoCode : BaseEntity
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public Guid? EventId { get; set; }

    [Required]
    public DiscountType DiscountType { get; set; }

    [Required, Column(TypeName = "decimal(10,2)")]
    public decimal DiscountValue { get; set; }

    public int? MaxUses { get; set; }

    [Required]
    public int CurrentUses { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; }

    [Required]
    public DateTime ValidTo { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(EventId))]
    public virtual Event? Event { get; set; }
}
