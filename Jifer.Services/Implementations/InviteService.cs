namespace Jifer.Services.Implementations
{
    using Jifer.Data.Models;
    using Jifer.Services.Interfaces;
    using System.Threading.Tasks;
    using Jifer.Data.Repositories;

    public class InviteService : IInviteService
    {
        private readonly IEmailService _emailService;

        private readonly IInviteGeneratorService _inviteGeneratorService;

        private readonly IRepository _repository;

        public InviteService(
            IEmailService emailService,
            IInviteGeneratorService inviteGeneratorService,
            IRepository repository)
        {
            this._emailService = emailService;
            this._inviteGeneratorService = inviteGeneratorService;
            this._repository = repository;
        }

        public async Task<bool> IsInvitationValidAsync(string email)
        {
            return await _inviteGeneratorService.IsInvitationValidAsync(email);
        }

        public async Task<string> GenerateInviteCodeAsync(JUser user, string email)
        {
            var invite = _inviteGeneratorService.GenerateInviteCodeAsync(user, email);

            await _repository.AddAsync(invite);

            await _repository.SaveChangesAsync();

            return invite.InvitationCode.ToString();
        }

        public async Task SendInvitationEmailAsync(string recipientEmail,string subject,string body)
        {
            await _emailService.SendEmailAsync(recipientEmail, subject, body);
        }
    }

}
