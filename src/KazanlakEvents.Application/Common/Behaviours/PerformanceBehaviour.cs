using System.Diagnostics;
using KazanlakEvents.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<PerformanceBehaviour<TRequest, TResponse>> logger,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();
        var response = await next();
        _timer.Stop();

        var elapsedMs = _timer.ElapsedMilliseconds;
        if (elapsedMs > 500)
        {
            logger.LogWarning(
                "KazanlakEvents Long Running Request: {Name} ({ElapsedMs}ms) by {UserId} {@Request}",
                typeof(TRequest).Name, elapsedMs, currentUser.UserId, request);
        }

        return response;
    }
}
