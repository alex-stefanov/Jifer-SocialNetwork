using System.Security.Claims;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KazanlakEvents.Web.Controllers.Api.v1;

[ApiController]
[Route("api/v1/webhooks")]
[Authorize(AuthenticationSchemes = "ApiJwt")]
[EnableRateLimiting("api")]
public class WebhooksApiController(IWebhookService webhookService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<WebhookSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(CancellationToken ct = default)
    {
        var userId = GetUserId();
        var subs = await webhookService.GetUserSubscriptionsAsync(userId, ct);
        return Ok(subs.Select(Map).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(WebhookSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateWebhookRequest request,
        CancellationToken ct = default)
    {
        if (!request.CallbackUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Callback URL must use HTTPS." });

        var userId = GetUserId();
        var sub = await webhookService.CreateSubscriptionAsync(
            userId, request.CallbackUrl, request.Events, ct);

        return CreatedAtAction(nameof(GetSubscriptions), null, Map(sub));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var subs = await webhookService.GetUserSubscriptionsAsync(userId, ct);

        if (subs.All(s => s.Id != id))
            return Forbid();

        await webhookService.DeleteSubscriptionAsync(id, ct);
        return NoContent();
    }


    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);

    private static WebhookSubscriptionDto Map(Domain.Entities.WebhookSubscription s) => new()
    {
        Id              = s.Id,
        CallbackUrl     = s.CallbackUrl,
        Events          = s.Events,
        IsActive        = s.IsActive,
        LastTriggeredAt = s.LastTriggeredAt,
        FailureCount    = s.FailureCount,
        CreatedAt       = s.CreatedAt
    };
}
