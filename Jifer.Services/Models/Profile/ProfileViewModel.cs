namespace Jifer.Services.Models.Profile
{
    using Jifer.Data.Models;

    public class ProfileViewModel
    {
        public JUser User { get; set; } = new JUser();

        public List<JUser> Friends { get; set; } = new List<JUser>();

        public List<JShip> SentFriendRequests { get; set; } = new List<JShip>();

        public List<JShip> ReceivedFriendRequests { get; set; } = new List<JShip>();

        public List<JInvitation> SentInvitations { get; set; } = new List<JInvitation>();

        public List<JGo> JGos { get; set; } = new List<JGo>();

        public bool IsFriendRequestSent { get; set; } = false;

        public bool HasPendingInvitation { get; set; } = false;

        public bool IsFriend { get; set; } = false;

        public bool IsFriendOfFriend { get; set; } = false;
    }
}
