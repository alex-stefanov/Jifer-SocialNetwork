using KazanlakEvents.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Infrastructure.Services.Email;

public class SmtpEmailService(ILogger<SmtpEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        // TODO: Implement actual SMTP sending in Sprint 5
        logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    public Task SendTemplateEmailAsync(string to, string templateName,
        Dictionary<string, string> placeholders, CancellationToken ct = default)
    {
        logger.LogInformation("Template email '{Template}' sent to {To}", templateName, to);
        return Task.CompletedTask;
    }
}
