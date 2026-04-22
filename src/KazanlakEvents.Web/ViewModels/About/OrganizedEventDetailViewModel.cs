namespace KazanlakEvents.Web.ViewModels.About;

public class OrganizedEventDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public DateTime? EventDate { get; set; }
    public int? AttendeesCount { get; set; }

    // Grouped by role in display order: Organizer, Coordinator, Volunteer, others
    public List<TeamRoleGroupViewModel> TeamGroups { get; set; } = [];

    public List<OrganizedEventSponsorViewModel> Sponsors { get; set; } = [];
}

public class TeamRoleGroupViewModel
{
    public string Role { get; set; } = string.Empty;
    public List<TeamMemberViewModel> Members { get; set; } = [];
}
