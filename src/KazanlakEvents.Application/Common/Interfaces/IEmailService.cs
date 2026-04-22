namespace KazanlakEvents.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendTemplateEmailAsync(string to, string templateName, Dictionary<string, string> placeholders, CancellationToken ct = default);
}
