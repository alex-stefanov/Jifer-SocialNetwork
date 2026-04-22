using System.Net;
using KazanlakEvents.Application.Common.Exceptions;

namespace KazanlakEvents.Web.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error occurred");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.Redirect("/Home/Error?message=Validation+error");
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.Redirect("/Home/Error?message=Not+found");
        }
        catch (ForbiddenAccessException ex)
        {
            logger.LogWarning(ex, "Forbidden access attempt");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.Redirect("/Account/AccessDenied");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
