namespace KazanlakEvents.Web.ViewModels.About;

public class AboutViewModel
{
    public int TotalEventsCount { get; set; }
    public int TotalAttendeesCount { get; set; }
    public int TotalVolunteersCount { get; set; }

    public List<OrganizedEventCardViewModel> OrganizedEvents { get; set; } = [];
    public List<TeamMemberViewModel> TeamMembers { get; set; } = [];
}
