namespace KazanlakEvents.Web.ViewModels.Api;

public class WebhookSubscriptionDto
{
    public Guid Id { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public string Events { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int FailureCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
