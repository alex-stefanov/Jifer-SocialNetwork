namespace Jifer.Services.Implementations
{
    using Jifer.Data.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Jifer.Data.Constants;
    using Jifer.Services.Interfaces;
    using Jifer.Data.Repositories;

    public class FriendHelperService : IFriendHelperService
    {
        private readonly IRepository _repository;

        public FriendHelperService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<JUser>> GetConfirmedFriendsAsync(JUser user)
        {
            var receivedFriendRequests = await _repository.AllReadonly<JShip>()
                .Where(fr => fr.ReceiverId == user.Id && fr.Status == ValidationConstants.FriendshipStatus.Confirmed && fr.IsActive)
                .Select(fr => fr.Sender)
                .ToListAsync();

            var sentFriendRequests = await _repository.AllReadonly<JShip>()
                .Where(fr => fr.SenderId == user.Id && fr.Status == ValidationConstants.FriendshipStatus.Confirmed && fr.IsActive)
                .Select(fr => fr.Receiver)
                .ToListAsync();

            var friends = receivedFriendRequests.Concat(sentFriendRequests)
                .Distinct()
                .OrderBy(f => f.UserName)
                .ToList();

            return friends;
        }

        public async Task<bool> IsUserFriendOfFriendsAsync(JUser user, JUser currentUser)
        {
            var friends = await GetConfirmedFriendsAsync(user);

            foreach (var friend in friends)
            {
                var friendsOfFriend = await GetConfirmedFriendsAsync(friend);
                if (friendsOfFriend.Any(ff => ff.Id == currentUser.Id))
                {
                    return true;
                }
            }

            return false;
        }
    }

}
