namespace Jifer.Services.Interfaces
{
    using Jifer.Data.Models;

    public interface IFriendHelperService
    {
        Task<List<JUser>> GetConfirmedFriendsAsync(JUser user);

        Task<bool> IsUserFriendOfFriendsAsync(JUser user, JUser currentUser);

    }
}
