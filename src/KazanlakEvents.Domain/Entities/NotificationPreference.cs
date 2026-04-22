using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KazanlakEvents.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Domain.Entities;

[Table("NotificationPreferences")]
[Index(nameof(UserId), IsUnique = true)]
public class NotificationPreference : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public bool EmailOnNewEvent { get; set; } = true;

    [Required]
    public bool EmailOnTicketPurchase { get; set; } = true;

    [Required]
    public bool EmailOnEventReminder { get; set; } = true;

    [Required, MaxLength(20)]
    public string EmailDigestMode { get; set; } = "instant";

    [Required]
    public bool InAppEnabled { get; set; } = true;
}
