namespace Jifer.Services.Implementations
{
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;
    using Jifer.Services.Interfaces;
    using Microsoft.Extensions.Configuration;

    public class EmailService : IEmailService
    {
        private readonly string server;
        private readonly int port;
        private readonly string senderName;
        private readonly string senderEmail;
        private readonly string username;
        private readonly string password;
        private readonly bool enableSsl;

        public EmailService(IConfiguration configuration)
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

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using var smtpClient = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }

}
