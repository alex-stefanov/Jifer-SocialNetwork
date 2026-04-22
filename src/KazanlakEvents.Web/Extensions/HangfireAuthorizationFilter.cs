using Hangfire.Dashboard;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.Extensions;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole(UserRoles.SuperAdmin)
            || httpContext.User.IsInRole(UserRoles.Admin);
    }
}
