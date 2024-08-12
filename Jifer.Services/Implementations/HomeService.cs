namespace Jifer.Services.Implementations
{
    using Jifer.Data.Models;
    using Jifer.Services.Interfaces;
    using Microsoft.AspNetCore.Identity;

    public class HomeService : IHomeService
    {
        private readonly UserManager<JUser> _userManager;

        public HomeService(UserManager<JUser> userManager)
        {
            this._userManager = userManager;
        }

        public async Task<JUser?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user)
        {
            return await _userManager.GetUserAsync(user);
        }
    }
}
