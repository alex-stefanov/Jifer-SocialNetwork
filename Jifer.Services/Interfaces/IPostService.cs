namespace Jifer.Services.Interfaces
{
    using Jifer.Data.Models;
    using Jifer.Services.Models.Post;
    using System.Threading.Tasks;

    public interface IPostService
    {
        Task<JGo> CreateJGoAsync(JUser user, JGoViewModel model);

        Task<TimelineViewModel> GetJGoPageAsync(string userId, int page = 1);

        Task<bool> DeletePostAsync(int id, string userId);

        Task<MyJGosViewModel> GetMyJGosAsync(string userId, int page = 1);

        Task<bool> DeleteMyJGoAsync(int id, string userId);

        Task<bool> UpdateMyJGoAsync(int id, string newText, string userId);

        Task<MyJGosViewModel> GetOtherJGosAsync(string otherId, string currentUserId, int page = 1);

    }
}
