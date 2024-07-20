namespace Jifer.Services.Interfaces
{
    using Jifer.Data.Models;

    public interface IInviteGeneratorService
    {
        Task<bool> IsInvitationValidAsync(string email);

        Task<JInvitation> GenerateInviteCodeAsync(JUser sender, string friendEmail);
    }
}
