namespace KazanlakEvents.Web.ViewModels.About;

public class OrganizedEventSponsorViewModel
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
}
