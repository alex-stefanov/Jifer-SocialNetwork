namespace Jifer.Services.Implementations
{
    using Jifer.Data.Models;
    using Jifer.Data.Repositories;
    using Jifer.Services.Interfaces;
    using System;
    using System.Threading.Tasks;

    public class InviteGeneratorService : IInviteGeneratorService
    { 
        private readonly IRepository repository;

        public InviteGeneratorService(IRepository _repository)
        {
           repository = _repository;
        }

        public async Task<JInvitation> GenerateInviteCodeAsync(JUser sender, string friendEmail)
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
            

            await repository.AddAsync(invite);

            await repository.SaveChangesAsync();

            return invite;
        }

        public async Task<bool> IsInvitationValidAsync(string email)
        {
            return await repository.IsInvitationValidAsync(email);
        }
    }

}
