namespace KazanlakEvents.Web.ViewModels.About;

public class ManageTeamViewModel
{
    public Guid OrganizedEventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public List<TeamMemberViewModel> TeamMembers { get; set; } = [];
}
