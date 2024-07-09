using System.Net.Mail;
using System.Net;

namespace Jifer.Helpers
{
    public class EHelper
    {
        private readonly string server;
        private readonly int port;
        private readonly string senderName;
        private readonly string senderEmail;
        private readonly string username;
        private readonly string password;
        private readonly bool enableSsl;

        public EHelper(IConfiguration configuration)
        {
            var smtpSettings = configuration.GetSection("SmtpSettings");

            server = smtpSettings["Server"];
            port = int.Parse(smtpSettings["Port"]);
            senderName = smtpSettings["SenderName"];
            senderEmail = smtpSettings["SenderEmail"];
            username = smtpSettings["Username"];
            password = smtpSettings["Password"];
            enableSsl = bool.Parse(smtpSettings["EnableSsl"]);
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            
            using (var smtpClient = new SmtpClient(server, port))
            {

                smtpClient.Credentials = new NetworkCredential(username, password);
                smtpClient.EnableSsl = enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                

                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
            }
            
        }
    }
}
