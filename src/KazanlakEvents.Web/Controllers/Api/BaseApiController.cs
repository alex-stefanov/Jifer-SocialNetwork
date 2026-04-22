using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KazanlakEvents.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[EnableRateLimiting("api")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")!);

    protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
}
