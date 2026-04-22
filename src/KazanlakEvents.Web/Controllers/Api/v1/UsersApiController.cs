using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.Controllers.Api;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KazanlakEvents.Web.Controllers.Api.v1;

/// <summary>
/// User profiles, follow relationships, notifications, and role requests.
/// </summary>
[Route("api/v1/users")]
public class UsersApiController(
    IUserService userService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IRepository<OrganizerRequest> organizerRequestRepo,
    IRepository<BlogAuthorRequest> blogAuthorRequestRepo,
    IUnitOfWork uow) : BaseApiController
{

    /// <summary>Get the authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(typeof(UserProfileApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct = default)
    {
        var profile = await userService.GetProfileAsync(currentUser.UserId!.Value, ct);
        if (profile is null) return NotFound(new { error = "Profile not found" });

        var appUser = await userManager.FindByIdAsync(currentUser.UserId.Value.ToString());
        return Ok(await BuildProfileDtoAsync(profile, appUser, ct));
    }

    /// <summary>Update the authenticated user's profile.</summary>
    [HttpPut("me")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(typeof(UserProfileApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var profile = await userService.GetProfileAsync(currentUser.UserId!.Value, ct);
            if (profile is null) return NotFound(new { error = "Profile not found" });

            profile.FirstName     = request.FirstName;
            profile.LastName      = request.LastName;
            profile.Bio           = request.Bio;
            profile.AvatarUrl     = request.AvatarUrl;
            profile.DateOfBirth   = request.DateOfBirth;
            profile.City          = request.City;
            profile.PhoneNumber   = request.PhoneNumber;

            var updated = await userService.UpdateProfileAsync(profile, ct);
            var appUser = await userManager.FindByIdAsync(currentUser.UserId.Value.ToString());
            return Ok(await BuildProfileDtoAsync(updated, appUser, ct));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>Get a user's public profile by their GUID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserProfileApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct = default)
    {
        var profile = await userService.GetProfileAsync(id, ct);
        if (profile is null) return NotFound(new { error = "User not found" });

        var appUser = await userManager.FindByIdAsync(id.ToString());
        return Ok(await BuildProfileDtoAsync(profile, appUser, ct));
    }


    /// <summary>Follow a user.</summary>
    [HttpPost("{id:guid}/follow")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Follow(Guid id, CancellationToken ct = default)
    {
        if (id == currentUser.UserId)
            return BadRequest(new { error = "You cannot follow yourself." });

        try
        {
            await userService.FollowAsync(currentUser.UserId!.Value, id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Unfollow a user.</summary>
    [HttpDelete("{id:guid}/follow")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unfollow(Guid id, CancellationToken ct = default)
    {
        try
        {
            await userService.UnfollowAsync(currentUser.UserId!.Value, id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>
    /// Get a user's follower count.
    /// Full follower list requires a dedicated follow repository — returns count only for now.
    /// </summary>
    [HttpGet("{id:guid}/followers")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowers(Guid id, CancellationToken ct = default)
    {
        var count = await userService.GetFollowerCountAsync(id, ct);
        return Ok(new { userId = id, followerCount = count });
    }

    /// <summary>
    /// Get a user's following count.
    /// Full following list requires a dedicated follow repository — returns count only for now.
    /// </summary>
    [HttpGet("{id:guid}/following")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowing(Guid id, CancellationToken ct = default)
    {
        var count = await userService.GetFollowingCountAsync(id, ct);
        return Ok(new { userId = id, followingCount = count });
    }


    /// <summary>Get the current user's notifications (paginated).</summary>
    [HttpGet("me/notifications")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(typeof(ApiPagedResult<NotificationApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? filter = null,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var userId = currentUser.UserId!.Value;

        var notifications = await notificationService.GetUserNotificationsAsync(userId, page, pageSize, filter, ct);
        var total = await notificationService.GetTotalCountAsync(userId, filter, ct);

        return Ok(new ApiPagedResult<NotificationApiDto>
        {
            Items      = notifications.Select(MapNotification).ToList(),
            Page       = page,
            PageSize   = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>Mark a specific notification as read.</summary>
    [HttpPost("me/notifications/{id:guid}/read")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct = default)
    {
        try
        {
            await notificationService.MarkAsReadAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>
    /// Submit a request for an elevated role (Organizer or BlogAuthor).
    /// An admin must review and approve this request.
    /// </summary>
    [HttpPost("me/role-request")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestRole(
        [FromBody] RoleRequestApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.UserId!.Value;

            if (request.Role.Equals("Organizer", StringComparison.OrdinalIgnoreCase))
            {
                var existing = await organizerRequestRepo.FindAsync(
                    r => r.UserId == userId && !r.IsReviewed, ct);
                if (existing.Any())
                    return BadRequest(new { error = "You already have a pending organizer request." });

                var orgRequest = new OrganizerRequest
                {
                    UserId = userId,
                    Reason = request.Reason
                };
                await organizerRequestRepo.AddAsync(orgRequest, ct);
                await uow.SaveChangesAsync(ct);
                return StatusCode(StatusCodes.Status201Created, new { message = "Organizer request submitted." });
            }

            if (request.Role.Equals("BlogAuthor", StringComparison.OrdinalIgnoreCase))
            {
                var existing = await blogAuthorRequestRepo.FindAsync(
                    r => r.UserId == userId && !r.IsReviewed, ct);
                if (existing.Any())
                    return BadRequest(new { error = "You already have a pending blog author request." });

                var blogRequest = new BlogAuthorRequest
                {
                    UserId = userId,
                    Reason = request.Reason
                };
                await blogAuthorRequestRepo.AddAsync(blogRequest, ct);
                await uow.SaveChangesAsync(ct);
                return StatusCode(StatusCodes.Status201Created, new { message = "Blog author request submitted." });
            }

            return BadRequest(new { error = "Role must be 'Organizer' or 'BlogAuthor'." });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    private async Task<UserProfileApiDto> BuildProfileDtoAsync(
        UserProfile profile,
        ApplicationUser? appUser,
        CancellationToken ct)
    {
        var followerCount  = await userService.GetFollowerCountAsync(profile.UserId, ct);
        var followingCount = await userService.GetFollowingCountAsync(profile.UserId, ct);
        var organizedCount = await userService.GetOrganizedEventCountAsync(profile.UserId, ct);
        var attendedCount  = await userService.GetAttendedEventCountAsync(profile.UserId, ct);

        return new UserProfileApiDto
        {
            UserId             = profile.UserId,
            UserName           = appUser?.UserName ?? string.Empty,
            Email              = appUser?.Email ?? string.Empty,
            FirstName          = profile.FirstName,
            LastName           = profile.LastName,
            FullName           = profile.FullName,
            Bio                = profile.Bio,
            AvatarUrl          = profile.AvatarUrl,
            City               = profile.City,
            FollowerCount      = followerCount,
            FollowingCount     = followingCount,
            OrganizedEventCount = organizedCount,
            AttendedEventCount  = attendedCount
        };
    }

    private static NotificationApiDto MapNotification(Notification n) => new()
    {
        Id        = n.Id,
        Type      = n.Type.ToString(),
        Title     = n.Title,
        Message   = n.Message,
        LinkUrl   = n.LinkUrl,
        IsRead    = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
