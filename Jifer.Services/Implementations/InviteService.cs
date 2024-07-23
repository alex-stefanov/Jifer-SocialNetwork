namespace Jifer.Services.Implementations
{
    using Jifer.Data.Models;
    using Jifer.Services.Interfaces;
    using System.Threading.Tasks;
    using Jifer.Data.Repositories;

    public class InviteService : IInviteService
    {
        private readonly IEmailService emailService;

        private readonly IInviteGeneratorService inviteGeneratorService;

        private readonly IRepository repository;

        public InviteService(
            IEmailService _emailService,
            IInviteGeneratorService _inviteGeneratorService,
            IRepository _repository)
        {
            emailService = _emailService;
            inviteGeneratorService = _inviteGeneratorService;
            repository = _repository;
        }

        public async Task<bool> IsInvitationValidAsync(string email)
        {
            return await inviteGeneratorService.IsInvitationValidAsync(email);
        }

        public async Task<string> GenerateInviteCodeAsync(JUser user, string email)
        {
            var invite = inviteGeneratorService.GenerateInviteCodeAsync(user, email);

            await repository.AddAsync(invite);

            await repository.SaveChangesAsync();

            return invite.InvitationCode.ToString();
        }

        public async Task SendInvitationEmailAsync(string recipientEmail,string subject,string body)
        {
            await emailService.SendEmailAsync(recipientEmail, subject, body);
        }
    }

}
