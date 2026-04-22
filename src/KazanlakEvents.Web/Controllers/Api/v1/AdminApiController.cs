using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.Controllers.Api;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Web.Controllers.Api.v1;

/// <summary>
/// Admin-only endpoints: platform statistics, user management, and role-request approvals.
/// </summary>
[Route("api/v1/admin")]
[Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Admin,SuperAdmin")]
public class AdminApiController(
    IAdminService adminService,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IRepository<OrganizerRequest> organizerRequestRepo,
    IRepository<BlogAuthorRequest> blogAuthorRequestRepo,
    IUnitOfWork uow) : BaseApiController
{
    /// <summary>Get high-level platform statistics.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStatsApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
    {
        var stats = new AdminStatsApiDto
        {
            TotalUsers       = await adminService.GetTotalUsersCountAsync(ct),
            TotalEvents      = await adminService.GetTotalEventsCountAsync(ct),
            PendingApprovals = await adminService.GetPendingApprovalsCountAsync(ct),
            TotalTicketsSold = await adminService.GetTotalTicketsSoldAsync(ct)
        };
        return Ok(stats);
    }

    /// <summary>Get a paginated list of all users.</summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (1–100)</param>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiPagedResult<AdminUserApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var allUsers = await userManager.Users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var total = await userManager.Users.CountAsync(ct);
        var dtos  = new List<AdminUserApiDto>();

        foreach (var u in allUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            dtos.Add(new AdminUserApiDto
            {
                Id        = u.Id,
                UserName  = u.UserName ?? string.Empty,
                Email     = u.Email    ?? string.Empty,
                IsActive  = u.IsActive,
                CreatedAt = u.CreatedAt,
                Roles     = roles
            });
        }

        return Ok(new ApiPagedResult<AdminUserApiDto>
        {
            Items      = dtos,
            Page       = page,
            PageSize   = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>Assign a role to a user.</summary>
    [HttpPost("users/{id:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddRole(Guid id, string role)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound(new { error = "User not found" });

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    /// <summary>Remove a role from a user.</summary>
    [HttpDelete("users/{id:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, string role)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound(new { error = "User not found" });

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    /// <summary>Get all pending organizer and blog-author role requests.</summary>
    [HttpGet("organizer-requests")]
    [ProducesResponseType(typeof(List<OrganizerRequestApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganizerRequests(CancellationToken ct = default)
    {
        var orgRequests  = await organizerRequestRepo.FindAsync(r => !r.IsReviewed, ct);
        var blogRequests = await blogAuthorRequestRepo.FindAsync(r => !r.IsReviewed, ct);

        var result = new List<OrganizerRequestApiDto>();

        foreach (var r in orgRequests)
        {
            var user = await userManager.FindByIdAsync(r.UserId.ToString());
            result.Add(new OrganizerRequestApiDto
            {
                Id            = r.Id,
                UserId        = r.UserId,
                UserEmail     = user?.Email ?? string.Empty,
                RequestedRole = "Organizer",
                Reason        = r.Reason,
                IsApproved    = r.IsApproved,
                IsReviewed    = r.IsReviewed,
                CreatedAt     = r.CreatedAt
            });
        }

        foreach (var r in blogRequests)
        {
            var user = await userManager.FindByIdAsync(r.UserId.ToString());
            result.Add(new OrganizerRequestApiDto
            {
                Id            = r.Id,
                UserId        = r.UserId,
                UserEmail     = user?.Email ?? string.Empty,
                RequestedRole = "BlogAuthor",
                Reason        = r.Reason,
                IsApproved    = r.IsApproved,
                IsReviewed    = r.IsReviewed,
                CreatedAt     = r.CreatedAt
            });
        }

        return Ok(result.OrderByDescending(r => r.CreatedAt).ToList());
    }

    /// <summary>Approve a role request by its ID. Grants the requested role to the user.</summary>
    [HttpPost("organizer-requests/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveOrganizerRequest(Guid id, CancellationToken ct = default)
    {
        var orgReq = await organizerRequestRepo.GetByIdAsync(id, ct);
        if (orgReq is not null)
        {
            orgReq.IsReviewed   = true;
            orgReq.IsApproved   = true;
            orgReq.ReviewedById = currentUser.UserId;
            orgReq.ReviewedAt   = DateTime.UtcNow;
            organizerRequestRepo.Update(orgReq);
            await uow.SaveChangesAsync(ct);

            var user = await userManager.FindByIdAsync(orgReq.UserId.ToString());
            if (user is not null) await userManager.AddToRoleAsync(user, "Organizer");
            return NoContent();
        }

        var blogReq = await blogAuthorRequestRepo.GetByIdAsync(id, ct);
        if (blogReq is not null)
        {
            blogReq.IsReviewed   = true;
            blogReq.IsApproved   = true;
            blogReq.ReviewedById = currentUser.UserId;
            blogReq.ReviewedAt   = DateTime.UtcNow;
            blogAuthorRequestRepo.Update(blogReq);
            await uow.SaveChangesAsync(ct);

            var user = await userManager.FindByIdAsync(blogReq.UserId.ToString());
            if (user is not null) await userManager.AddToRoleAsync(user, "BlogAuthor");
            return NoContent();
        }

        return NotFound(new { error = "Request not found" });
    }

    /// <summary>Reject a role request by its ID.</summary>
    [HttpPost("organizer-requests/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectOrganizerRequest(Guid id, CancellationToken ct = default)
    {
        var orgReq = await organizerRequestRepo.GetByIdAsync(id, ct);
        if (orgReq is not null)
        {
            orgReq.IsReviewed   = true;
            orgReq.IsApproved   = false;
            orgReq.ReviewedById = currentUser.UserId;
            orgReq.ReviewedAt   = DateTime.UtcNow;
            organizerRequestRepo.Update(orgReq);
            await uow.SaveChangesAsync(ct);
            return NoContent();
        }

        var blogReq = await blogAuthorRequestRepo.GetByIdAsync(id, ct);
        if (blogReq is not null)
        {
            blogReq.IsReviewed   = true;
            blogReq.IsApproved   = false;
            blogReq.ReviewedById = currentUser.UserId;
            blogReq.ReviewedAt   = DateTime.UtcNow;
            blogAuthorRequestRepo.Update(blogReq);
            await uow.SaveChangesAsync(ct);
            return NoContent();
        }

        return NotFound(new { error = "Request not found" });
    }
}
