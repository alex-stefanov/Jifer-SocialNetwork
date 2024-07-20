namespace Jifer.Services.Implementations
{
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;
    using Jifer.Services.Interfaces;
    using Microsoft.Extensions.Configuration;

    public class EmailService : IEmailService
    {
        private readonly string _server;
        private readonly int _port;
        private readonly string _senderName;
        private readonly string _senderEmail;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration)
        {
            var smtpSettings = configuration.GetSection("SmtpSettings");

            _server = smtpSettings["Server"];
            _port = int.Parse(smtpSettings["Port"]);
            _senderName = smtpSettings["SenderName"];
            _senderEmail = smtpSettings["SenderEmail"];
            _username = smtpSettings["Username"];
            _password = smtpSettings["Password"];
            _enableSsl = bool.Parse(smtpSettings["EnableSsl"]);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using var smtpClient = new SmtpClient(_server, _port)
            {
                Credentials = new NetworkCredential(_username, _password),
                EnableSsl = _enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }

}
