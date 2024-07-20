namespace Jifer.Services.Interfaces
{
    using Jifer.Services.Models.Register;
    using Jifer.Services.Models.Login;
    using Jifer.Services.Models.Profile;
    using System.Threading.Tasks;

    public interface IUserService
    {
        Task RegisterUserAsync(RegisterViewModel model);
        Task LoginUserAsync(LoginViewModel model);
        Task LogoutUserAsync();
        Task<ProfileViewModel> GetProfileAsync(string userId);
        Task<ProfileViewModel> GetOtherProfileAsync(string userId, string otherId);
        Task SendFriendRequestAsync(string otherId);
        Task CancelFriendRequestAsync(string otherId);
        Task AcceptFriendRequestAsync(string otherId);
        Task DeclineFriendRequestAsync(string otherId);
        Task RemoveFriendshipAsync(string otherId);
    }
}
