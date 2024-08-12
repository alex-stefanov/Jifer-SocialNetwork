namespace Jifer.Services.Implementations
{
    using Jifer.Data.Constants;
    using Jifer.Data.Models;
    using Jifer.Data.Repositories;
    using Jifer.Services.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class FriendHelperService : IFriendHelperService
    {
        private readonly IRepository _repository;

        public FriendHelperService(IRepository repository)
        {
            this._repository = repository;
        }

        public async Task<List<JUser>> GetConfirmedFriendsAsync(JUser user)
        {
            var receivedFriendRequests = await _repository.AllReadonly<JShip>()
                .Include(fr=>fr.Sender)
                .Where(fr => fr.ReceiverId == user.Id && fr.Status == ValidationConstants.FriendshipStatus.Confirmed && fr.IsActive)
                .Select(fr => fr.Sender)
                .ToListAsync();

            var sentFriendRequests = await _repository.AllReadonly<JShip>()
                .Include(fr=>fr.Receiver)
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
