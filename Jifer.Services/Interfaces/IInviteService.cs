namespace Jifer.Services.Interfaces
{
    using Jifer.Data.Models;

    public interface IInviteService
    {
        Task<bool> IsInvitationValidAsync(string email);

        Task<string> GenerateInviteCodeAsync(JUser user, string email);

        Task SendInvitationEmailAsync(string recipientEmail, string subject, string body);

    }
}
