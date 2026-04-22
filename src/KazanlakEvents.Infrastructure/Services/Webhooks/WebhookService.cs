using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hangfire;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Infrastructure.Services.Webhooks;

public class WebhookService(
    IApplicationDbContext db,
    IUnitOfWork unitOfWork,
    HttpClient httpClient,
    ILogger<WebhookService> logger) : IWebhookService
{
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<WebhookSubscription> CreateSubscriptionAsync(
        Guid userId, string callbackUrl, string events, CancellationToken ct = default)
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var subscription = new WebhookSubscription
        {
            UserId      = userId,
            CallbackUrl = callbackUrl,
            Secret      = secret,
            Events      = events,
            IsActive    = true,
            FailureCount = 0
        };

        db.WebhookSubscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Webhook subscription {Id} created for user {UserId} → {Url}",
            subscription.Id, userId, callbackUrl);

        return subscription;
    }

    public async Task DeleteSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var subscription = await db.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == subscriptionId, ct);

        if (subscription == null) return;

        db.WebhookSubscriptions.Remove(subscription);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetUserSubscriptionsAsync(
        Guid userId, CancellationToken ct = default)
        => await db.WebhookSubscriptions
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

    public Task DispatchAsync(string eventType, object payload, CancellationToken ct = default)
    {
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        BackgroundJob.Enqueue<IWebhookService>(svc => svc.ExecuteDispatchAsync(eventType, payloadJson));
        return Task.CompletedTask;
    }

    public async Task ExecuteDispatchAsync(string eventType, string payloadJson)
    {
        var subscriptions = await db.WebhookSubscriptions
            .Where(w => w.IsActive && w.Events.Contains(eventType))
            .ToListAsync();

        foreach (var sub in subscriptions)
        {
            await DeliverAsync(sub, eventType, payloadJson);
        }
    }

    private async Task DeliverAsync(
        WebhookSubscription subscription, string eventType, string payloadJson)
    {
        var signature = ComputeSignature(payloadJson, subscription.Secret);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.CallbackUrl);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("X-Webhook-Event", eventType);
            request.Headers.TryAddWithoutValidation("X-Webhook-Signature", signature);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await httpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                subscription.FailureCount    = 0;
                subscription.LastTriggeredAt = DateTime.UtcNow;

                logger.LogDebug(
                    "Webhook {Id} delivered {EventType} → {StatusCode}",
                    subscription.Id, eventType, (int)response.StatusCode);
            }
            else
            {
                await HandleFailureAsync(subscription, eventType,
                    $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(subscription, eventType, ex.Message);
        }

        await unitOfWork.SaveChangesAsync();
    }

    private async Task HandleFailureAsync(
        WebhookSubscription subscription, string eventType, string reason)
    {
        subscription.FailureCount++;

        if (subscription.FailureCount >= 5)
        {
            subscription.IsActive = false;
            logger.LogWarning(
                "Webhook {Id} auto-disabled after 5 consecutive failures. Last event: {EventType}",
                subscription.Id, eventType);
        }
        else
        {
            logger.LogWarning(
                "Webhook {Id} delivery failed ({Count}/5) for {EventType}: {Reason}",
                subscription.Id, subscription.FailureCount, eventType, reason);
        }

        await Task.CompletedTask; // SaveChangesAsync called by caller
    }

    private static string ComputeSignature(string payload, string secret)
    {
        var keyBytes     = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
