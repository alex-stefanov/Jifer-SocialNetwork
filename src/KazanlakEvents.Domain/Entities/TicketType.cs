using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("TicketTypes")]
[Index(nameof(EventId), nameof(Name), IsUnique = true)]
public class TicketType : BaseEntity
{
    [Required]
    public Guid EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, Column(TypeName = "decimal(10,2)"), Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int QuantitySold { get; set; }

    public DateTime? SalesStartDate { get; set; }
    public DateTime? SalesEndDate { get; set; }

    [Required, Range(1, 100)]
    public int MaxPerOrder { get; set; } = 10;

    [Required]
    public int SortOrder { get; set; }

    [NotMapped]
    public int AvailableQuantity => Quantity - QuantitySold;

    [NotMapped]
    public bool IsAvailable => QuantitySold < Quantity
        && (SalesStartDate == null || DateTime.UtcNow >= SalesStartDate)
        && (SalesEndDate == null || DateTime.UtcNow <= SalesEndDate);

    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
