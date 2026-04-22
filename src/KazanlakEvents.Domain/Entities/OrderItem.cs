using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;

namespace KazanlakEvents.Domain.Entities;

[Table("OrderItems")]
public class OrderItem : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid TicketTypeId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Required, Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [ForeignKey(nameof(OrderId))]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey(nameof(TicketTypeId))]
    public virtual TicketType TicketType { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
