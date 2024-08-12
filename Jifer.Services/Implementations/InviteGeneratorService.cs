namespace Jifer.Services.Implementations
{
    using Jifer.Data.Models;
    using Jifer.Data.Repositories;
    using Jifer.Services.Interfaces;
    using System;
    using System.Threading.Tasks;

    public class InviteGeneratorService : IInviteGeneratorService
    { 
        private readonly IRepository _repository;

        public InviteGeneratorService(IRepository repository)
        {
           this._repository = repository;
        }

        public JInvitation GenerateInviteCodeAsync(JUser sender, string friendEmail)
        {
            var code = Guid.NewGuid();
            var expirationTime = DateTime.Now.AddDays(2);

            var invite = new JInvitation()
            {
                Sender=sender,
                InviteeEmail=friendEmail,
                CreationDate=DateTime.Now,
                ExpirationDate=expirationTime,
                SenderId = sender.Id,
                InvitationCode = code
            };

            return invite;
        }

        public async Task<bool> IsInvitationValidAsync(string email)
        {
            return await _repository.IsInvitationValidAsync(email);
        }
    }

}
