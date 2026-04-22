using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface IWebhookService
{
    Task<WebhookSubscription> CreateSubscriptionAsync(
        Guid userId, string callbackUrl, string events, CancellationToken ct = default);

    Task DeleteSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);

    Task<IReadOnlyList<WebhookSubscription>> GetUserSubscriptionsAsync(
        Guid userId, CancellationToken ct = default);

    Task DispatchAsync(string eventType, object payload, CancellationToken ct = default);

    // Must be public — Hangfire requires it to serialize/deserialize the job by method signature.
    Task ExecuteDispatchAsync(string eventType, string payloadJson);
}
