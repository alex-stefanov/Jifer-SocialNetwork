namespace Jifer.Helpers
{
    using Microsoft.EntityFrameworkCore;
    using Jifer.Data.Models;
    using Jifer.Data.Constants;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Jifer.Data;

    public class UHelper
    {
        private readonly ApplicationDbContext _context;

        public UHelper(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<JUser>> GetConfirmedFriendsAsync(JUser user)
        {
            await _context.Entry(user)
                .Collection(u => u.ReceivedFriendRequests)
                .Query()
                .Include(fr => fr.Sender)
                .Where(fr => fr.Status == ValidationConstants.FriendshipStatus.Confirmed && fr.IsActive)
                .LoadAsync();

            await _context.Entry(user)
                .Collection(u => u.SentFriendRequests)
                .Query()
                .Include(fr => fr.Receiver)
                .Where(fr => fr.Status == ValidationConstants.FriendshipStatus.Confirmed && fr.IsActive)
                .LoadAsync();

            var friends = user.ReceivedFriendRequests
                .Where(fr => fr.Sender != null)
                .Select(fr => fr.Sender)
                .ToList();

            friends.AddRange(user.SentFriendRequests
                .Where(fr => fr.Receiver != null)
                .Select(fr => fr.Receiver)
                .ToList());

            return friends.Distinct().OrderBy(f => f.UserName).ToList();
        }

        public async Task<bool> IsUserFriendOfFriendsAsync(JUser user, JUser currentUser)
        {
            var friends = await GetConfirmedFriendsAsync(user);

            foreach (var friend in friends)
            {
                var friendsOfFriend = await GetConfirmedFriendsAsync(friend);

                if (friendsOfFriend.Contains(currentUser))
                {
                    return true;
                }
            }

            return false;
        }
    }

}
