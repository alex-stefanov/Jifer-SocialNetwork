using Jifer.Data;
using Jifer.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Jifer.Helpers
{
    public class IHelper
    {
        private readonly ApplicationDbContext _context;
        
        public IHelper(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<JInvitation> GenerateInviteCode(JUser sender,string friendEmail)
        {
            var code = Guid.NewGuid();
            var expirationTime = DateTime.Now.AddDays(2);

            var invite = new JInvitation(sender,friendEmail,expirationTime)
            {
                SenderId= sender.Id,
                InvitationCode = code
            };

            _context.Invitations.Add(invite);
            _context.SaveChanges();

            return invite;
        }
    }

}
