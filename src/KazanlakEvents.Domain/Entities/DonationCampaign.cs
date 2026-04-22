using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("DonationCampaigns")]
[Index(nameof(Slug), IsUnique = true)]
public class DonationCampaign : BaseEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal GoalAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CurrentAmount { get; set; } = 0;

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
}
