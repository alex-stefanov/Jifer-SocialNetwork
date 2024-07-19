using Jifer.Data.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Jifer.Models.Profile
{
    public class ProfileViewModel
    {
        public JUser User { get; set; }

        public List<JUser> Friends { get; set; }

        public List<JShip> SentFriendRequests { get; set; }

        public List<JShip> ReceivedFriendRequests { get; set; }

        public List<JInvitation> SentInvitations { get; set; }

        public List<JGo> JGos { get; set; }

        public bool IsFriendRequestSent { get; set; } = false;

        public bool HasPendingInvitation { get; set; } = false;

        public bool IsFriend { get; set; } = false;

        public bool IsFriendOfFriend { get; set; } = false;
    }
}
