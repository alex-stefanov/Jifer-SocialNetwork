using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Volunteer;

public class PhoneRequiredViewModel
{
    public Guid ShiftId { get; set; }
    public Guid EventId { get; set; }

    [Required]
    [Phone]
    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;
}
