using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("Categories")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(Name), IsUnique = true)]
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? IconCssClass { get; set; }

    [Required]
    public int SortOrder { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
