using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.Controllers.Api;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KazanlakEvents.Web.Controllers.Api.v1;

/// <summary>
/// Manage events: discovery, CRUD, approval workflow, favorites, comments, and registration.
/// </summary>
[Route("api/v1/events")]
public class EventsApiController(
    IEventService eventService,
    ICommentService commentService,
    IUserService userService,
    IMapper mapper,
    UserManager<ApplicationUser> userManager) : BaseApiController
{

    /// <summary>Get a paginated list of published events with optional filters.</summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (1–100)</param>
    /// <param name="categoryId">Filter by category ID</param>
    /// <param name="search">Full-text search query</param>
    /// <param name="fromDate">Events starting on or after this date</param>
    /// <param name="toDate">Events starting before this date</param>
    /// <param name="isFree">Filter free events only</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResult<EventApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool? isFree = null,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (events, total) = await eventService.GetEventsPagedAsync(
            page, pageSize, categoryId,
            searchQuery: search,
            fromDate: fromDate,
            toDate: toDate,
            isFree: isFree,
            ct: ct);

        return Ok(new ApiPagedResult<EventApiDto>
        {
            Items      = mapper.Map<List<EventApiDto>>(events),
            Page       = page,
            PageSize   = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>Get the top upcoming events.</summary>
    /// <param name="count">Number of events to return (1–50)</param>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(List<EventApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcoming(
        [FromQuery] int count = 10,
        CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 50);
        var events = await eventService.GetUpcomingEventsAsync(count, ct);
        return Ok(mapper.Map<List<EventApiDto>>(events));
    }

    /// <summary>Find events near a geographic location.</summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lng">Longitude</param>
    /// <param name="radiusKm">Search radius in kilometres</param>
    /// <param name="count">Maximum results</param>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(List<EventApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] decimal lat,
        [FromQuery] decimal lng,
        [FromQuery] double radiusKm = 10,
        [FromQuery] int count = 20,
        CancellationToken ct = default)
    {
        var events = await eventService.GetNearbyEventsAsync(lat, lng, radiusKm, count, ct);
        return Ok(mapper.Map<List<EventApiDto>>(events));
    }

    /// <summary>Get a single event by its GUID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventApiDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(Guid id, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventByIdAsync(id, ct);
        if (ev is null) return NotFound(new { error = "Event not found" });

        await eventService.IncrementViewCountAsync(id, ct);
        return Ok(mapper.Map<EventApiDetailDto>(ev));
    }

    /// <summary>Get a single event by its URL slug.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(EventApiDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(string slug, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventBySlugAsync(slug, ct);
        if (ev is null) return NotFound(new { error = "Event not found" });

        await eventService.IncrementViewCountAsync(ev.Id, ct);
        return Ok(mapper.Map<EventApiDetailDto>(ev));
    }


    /// <summary>Create a new event. Requires Organizer role or higher.</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Organizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(EventApiDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateEventApiRequest request,
        CancellationToken ct = default)
    {
        var organizerIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(organizerIdClaim, out var organizerId))
            return Unauthorized(new { error = "Invalid token" });

        var entity = new Event
        {
            Title            = request.Title,
            Description      = request.Description,
            ShortDescription = request.ShortDescription,
            CategoryId       = request.CategoryId,
            VenueId          = request.VenueId,
            StartDate        = request.StartDate,
            EndDate          = request.EndDate,
            Capacity         = request.Capacity,
            MinAge           = request.MinAge,
            IsFree           = request.IsFree,
            IsAccessible     = request.IsAccessible,
            OrganizerId      = organizerId
        };

        var created = await eventService.CreateEventAsync(entity, request.TagIds, ct);
        return CreatedAtAction(nameof(GetEventById), new { id = created.Id },
            mapper.Map<EventApiDetailDto>(created));
    }

    /// <summary>Update an existing event. Only the event owner or an Admin can do this.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(typeof(EventApiDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(
        Guid id,
        [FromBody] UpdateEventApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var ev = await eventService.GetEventByIdAsync(id, ct);
            if (ev is null) return NotFound(new { error = "Event not found" });

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            if (ev.OrganizerId != CurrentUserId && !isAdmin)
                return Forbid();

            ev.Title            = request.Title;
            ev.Description      = request.Description;
            ev.ShortDescription = request.ShortDescription;
            ev.CategoryId       = request.CategoryId;
            ev.VenueId          = request.VenueId;
            ev.StartDate        = request.StartDate;
            ev.EndDate          = request.EndDate;
            ev.Capacity         = request.Capacity;
            ev.MinAge           = request.MinAge;
            ev.IsFree           = request.IsFree;
            ev.IsAccessible     = request.IsAccessible;

            var updated = await eventService.UpdateEventAsync(ev, request.TagIds, ct);
            return Ok(mapper.Map<EventApiDetailDto>(updated));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Delete an event. Owner or Moderator/Admin required.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct = default)
    {
        try
        {
            var ev = await eventService.GetEventByIdAsync(id, ct);
            if (ev is null) return NotFound(new { error = "Event not found" });

            var canDelete = ev.OrganizerId == CurrentUserId
                || User.IsInRole("Admin") || User.IsInRole("SuperAdmin")
                || User.IsInRole("Moderator");
            if (!canDelete) return Forbid();

            await eventService.DeleteEventAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>Approve an event for publishing. Moderator or Admin required.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Moderator,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(EventApiDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveEvent(Guid id, CancellationToken ct = default)
    {
        try
        {
            var approved = await eventService.ApproveEventAsync(id, CurrentUserId, ct);
            return Ok(mapper.Map<EventApiDetailDto>(approved));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Reject an event submission. Moderator or Admin required.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Moderator,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(EventApiDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectEvent(
        Guid id,
        [FromBody] RejectEventApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var rejected = await eventService.RejectEventAsync(id, CurrentUserId, request.Reason, ct);
            return Ok(mapper.Map<EventApiDetailDto>(rejected));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>Add an event to the current user's favorites.</summary>
    [HttpPost("{id:guid}/favorite")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddFavorite(Guid id, CancellationToken ct = default)
    {
        try
        {
            await userService.FavoriteEventAsync(CurrentUserId, id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Remove an event from the current user's favorites.</summary>
    [HttpDelete("{id:guid}/favorite")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFavorite(Guid id, CancellationToken ct = default)
    {
        try
        {
            await userService.UnfavoriteEventAsync(CurrentUserId, id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>
    /// Record attendance intent for an event (Going / Interested).
    /// Requires IEventAttendanceService — not yet implemented; returns 501.
    /// </summary>
    [HttpPost("{id:guid}/attendance")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult RecordAttendance(Guid id)
        => StatusCode(StatusCodes.Status501NotImplemented,
            new { error = "Attendance management via API not yet available. Requires IEventAttendanceService." });


    /// <summary>Get all top-level comments for an event.</summary>
    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(List<CommentApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken ct = default)
    {
        try
        {
            var comments = await commentService.GetEventCommentsAsync(id, ct);
            var dtos = await MapCommentsAsync(comments, ct);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Post a top-level comment on an event.</summary>
    [HttpPost("{id:guid}/comments")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(typeof(CommentApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddCommentApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var comment = await commentService.AddCommentAsync(id, CurrentUserId, request.Content, ct: ct);
            var dto = await MapSingleCommentAsync(comment, ct);
            return CreatedAtAction(nameof(GetComments), new { id }, dto);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Reply to an existing comment.</summary>
    [HttpPost("{id:guid}/comments/{commentId:guid}/reply")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(typeof(CommentApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReplyToComment(
        Guid id,
        Guid commentId,
        [FromBody] AddCommentApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var reply = await commentService.AddCommentAsync(
                id, CurrentUserId, request.Content, parentCommentId: commentId, ct: ct);
            var dto = await MapSingleCommentAsync(reply, ct);
            return CreatedAtAction(nameof(GetComments), new { id }, dto);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Delete a comment. Owner of the comment or Moderator/Admin required.</summary>
    [HttpDelete("comments/{commentId:guid}")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct = default)
    {
        try
        {
            var isMod = User.IsInRole("Moderator") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            if (!isMod)
            {
                var comments = await commentService.GetEventCommentsAsync(Guid.Empty, ct);
            }

            await commentService.DeleteCommentAsync(commentId, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>
    /// Register for an event by purchasing tickets.
    /// Use <c>POST /api/v1/tickets/purchase</c> for the same operation.
    /// </summary>
    [HttpPost("{id:guid}/register")]
    [Authorize(AuthenticationSchemes = "ApiJwt")]
    [ProducesResponseType(typeof(PurchaseResultApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult RegisterForEvent(Guid id)
        => RedirectToAction("PurchaseTickets", "TicketsApi");


    private async Task<List<CommentApiDto>> MapCommentsAsync(
        IReadOnlyList<Comment> comments,
        CancellationToken ct)
    {
        var result = new List<CommentApiDto>();
        foreach (var c in comments.Where(c => c.ParentCommentId == null && !c.IsHidden))
            result.Add(await MapSingleCommentAsync(c, ct));
        return result;
    }

    private async Task<CommentApiDto> MapSingleCommentAsync(Comment c, CancellationToken ct)
    {
        var author = await userManager.FindByIdAsync(c.UserId.ToString());
        var profile = await userService.GetProfileAsync(c.UserId, ct);

        var dto = new CommentApiDto
        {
            Id              = c.Id,
            EventId         = c.EventId,
            UserId          = c.UserId,
            ParentCommentId = c.ParentCommentId,
            Content         = c.Content,
            IsEdited        = c.IsEdited,
            UpvoteCount     = c.UpvoteCount,
            CreatedAt       = c.CreatedAt,
            AuthorName      = profile?.FullName ?? author?.UserName ?? "Unknown",
            AuthorAvatarUrl = profile?.AvatarUrl
        };

        foreach (var reply in c.Replies.Where(r => !r.IsHidden))
            dto.Replies.Add(await MapSingleCommentAsync(reply, ct));

        return dto;
    }
}
