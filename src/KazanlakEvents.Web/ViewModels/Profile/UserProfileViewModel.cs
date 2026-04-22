using KazanlakEvents.Web.ViewModels.Event;
using KazanlakEvents.Web.ViewModels.Volunteer;

namespace KazanlakEvents.Web.ViewModels.Profile;

public class UserProfileViewModel
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PreferredLanguage { get; set; }
    public DateTime MemberSince { get; set; }

    public int EventsOrganized { get; set; }
    public int EventsAttended { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int VolunteerHours { get; set; }

    public List<EventCardViewModel> OrganizedEvents { get; set; } = new();
    public List<EventCardViewModel> FavoriteEvents { get; set; } = new();
    public List<TicketViewModel> UpcomingTickets { get; set; } = new();
    public List<TicketViewModel> PastTickets { get; set; } = new();
    public List<VolunteerSignupViewModel> VolunteerSignups { get; set; } = new();

    public bool IsOwnProfile { get; set; }
    public bool IsFollowing { get; set; }
}
