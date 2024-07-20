namespace Jifer.Services.Interfaces
{
    using Jifer.Data.Models;

    public interface IHomeService
    {
        Task<JUser?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user);
    }
}
