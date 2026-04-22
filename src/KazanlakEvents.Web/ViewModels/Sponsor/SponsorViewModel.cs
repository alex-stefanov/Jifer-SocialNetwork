using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Sponsor;

public class SponsorViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Description { get; set; }
    public SponsorTier Tier { get; set; }
    public bool IsActive { get; set; }
    public int ImpressionCount { get; set; }
    public int ClickCount { get; set; }
    public int EventsSponsoredCount { get; set; }
}
