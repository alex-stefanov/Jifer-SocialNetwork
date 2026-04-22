using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using KazanlakEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Orders")]
[Index(nameof(OrderNumber), IsUnique = true)]
[Index(nameof(UserId), nameof(CreatedAt), Name = "IX_Orders_UserId_CreatedAt")]
public class Order : BaseEntity
{
    [Required, MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid EventId { get; set; }

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required, Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required, Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public Guid? PromoCodeId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
