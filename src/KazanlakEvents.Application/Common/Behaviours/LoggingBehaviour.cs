using KazanlakEvents.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUser.UserId?.ToString() ?? "Anonymous";

        logger.LogInformation("KazanlakEvents Request: {Name} by {UserId} {@Request}",
            requestName, userId, request);

        var response = await next();

        logger.LogInformation("KazanlakEvents Response: {Name} by {UserId}",
            requestName, userId);

        return response;
    }
}
