using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class CreateWebhookRequest
{
    [Required, MaxLength(500), Url]
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of event types to subscribe to.
    /// Supported: event.created, event.published, event.cancelled, event.approved
    /// </summary>
    [Required, MaxLength(500)]
    public string Events { get; set; } = string.Empty;
}
