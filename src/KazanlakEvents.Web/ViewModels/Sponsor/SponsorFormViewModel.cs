using System.ComponentModel.DataAnnotations;
using KazanlakEvents.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KazanlakEvents.Web.ViewModels.Sponsor;

public class SponsorFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public SponsorTier Tier { get; set; }

    public bool IsActive { get; set; } = true;

    public string? ExistingLogoUrl { get; set; }
    public IFormFile? LogoFile { get; set; }

    public List<SelectListItem> Tiers { get; set; } = new();
}
