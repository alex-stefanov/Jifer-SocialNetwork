using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("UserProfiles")]
[Index(nameof(UserId), IsUnique = true)]
public class UserProfile : AuditableEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Bio { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required, MaxLength(5)]
    public string PreferredLanguage { get; set; } = "bg";

    public bool IsTrustedAuthor { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
