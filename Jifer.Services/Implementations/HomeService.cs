namespace Jifer.Services.Implementations
{
    using Jifer.Data.Models;
    using Jifer.Services.Interfaces;
    using Microsoft.AspNetCore.Identity;

    public class HomeService : IHomeService
    {
        private readonly UserManager<JUser> userManager;

        public HomeService(UserManager<JUser> _userManager)
        {
            this.userManager = _userManager;
        }

        public async Task<JUser?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user)
        {
            return await userManager.GetUserAsync(user);
        }
    }
}
