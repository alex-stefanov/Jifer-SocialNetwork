using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Web.Controllers.Api;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KazanlakEvents.Web.Controllers.Api.v1;

/// <summary>
/// Volunteer task and shift management, sign-ups, status updates, and hour logging.
/// </summary>
[Route("api/v1/volunteer")]
[Authorize(AuthenticationSchemes = "ApiJwt")]
public class VolunteerApiController(
    IVolunteerService volunteerService,
    ICurrentUserService currentUser) : BaseApiController
{

    /// <summary>Get all volunteer tasks (with shifts) for a specific event.</summary>
    [HttpGet("tasks/{eventId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<VolunteerTaskApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasksForEvent(Guid eventId, CancellationToken ct = default)
    {
        var tasks = await volunteerService.GetEventTasksAsync(eventId, ct);
        return Ok(tasks.Select(MapTask).ToList());
    }


    /// <summary>Sign up for a volunteer shift.</summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(VolunteerSignupApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignUp(
        [FromBody] SignUpApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var signup = await volunteerService.SignUpAsync(
                request.ShiftId, currentUser.UserId!.Value, ct);
            return CreatedAtAction(nameof(GetTasksForEvent), null, MapSignup(signup));
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

    /// <summary>Cancel / remove a volunteer sign-up.</summary>
    [HttpDelete("signup/{signupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSignup(Guid signupId, CancellationToken ct = default)
    {
        try
        {
            var (volunteerUserId, _, _) = await volunteerService.RemoveSignupAsync(signupId, ct);

            var isAdmin = currentUser.IsInRole("Admin") || currentUser.IsInRole("SuperAdmin");
            if (volunteerUserId != currentUser.UserId && !isAdmin)
                return Forbid();

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Signup not found" });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>Update the status of a volunteer sign-up. Organizer or Admin required.</summary>
    [HttpPost("signup/{signupId:guid}/status")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Organizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(VolunteerSignupApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSignupStatus(
        Guid signupId,
        [FromBody] SignupStatusApiRequest request,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<VolunteerSignupStatus>(request.Status, true, out var status))
            return BadRequest(new { error = $"Invalid status '{request.Status}'. Valid values: {string.Join(", ", Enum.GetNames<VolunteerSignupStatus>())}" });

        try
        {
            var updated = await volunteerService.UpdateSignupStatusAsync(signupId, status, ct);
            return Ok(MapSignup(updated));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Log hours for a volunteer sign-up. Organizer or Admin required.</summary>
    [HttpPost("signup/{signupId:guid}/hours")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Organizer,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogHours(
        Guid signupId,
        [FromBody] LogHoursApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            await volunteerService.LogHoursAsync(signupId, request.Hours, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Create a new volunteer task for an event. Organizer or Admin required.</summary>
    [HttpPost("task")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Organizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(VolunteerTaskApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTask(
        [FromBody] CreateVolunteerTaskApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var task = new VolunteerTask
            {
                EventId           = request.EventId,
                Name              = request.Name,
                Description       = request.Description,
                VolunteersNeeded  = request.VolunteersNeeded
            };

            var created = await volunteerService.CreateTaskAsync(task, ct);
            return CreatedAtAction(nameof(GetTasksForEvent),
                new { eventId = created.EventId }, MapTask(created));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Create a new shift under an existing volunteer task. Organizer or Admin required.</summary>
    [HttpPost("shift")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Organizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(VolunteerShiftApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateShift(
        [FromBody] CreateVolunteerShiftApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var shift = new VolunteerShift
            {
                TaskId        = request.TaskId,
                StartTime     = request.StartTime,
                EndTime       = request.EndTime,
                MaxVolunteers = request.MaxVolunteers
            };

            var created = await volunteerService.CreateShiftAsync(shift, ct);
            return CreatedAtAction(nameof(GetTasksForEvent), null, MapShift(created));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    private static VolunteerTaskApiDto MapTask(VolunteerTask t) => new()
    {
        Id               = t.Id,
        EventId          = t.EventId,
        Name             = t.Name,
        Description      = t.Description,
        VolunteersNeeded = t.VolunteersNeeded,
        Shifts           = t.Shifts.Select(MapShift).ToList()
    };

    private static VolunteerShiftApiDto MapShift(VolunteerShift s) => new()
    {
        Id            = s.Id,
        TaskId        = s.TaskId,
        StartTime     = s.StartTime,
        EndTime       = s.EndTime,
        MaxVolunteers = s.MaxVolunteers,
        SignedUpCount = s.Signups.Count
    };

    private static VolunteerSignupApiDto MapSignup(VolunteerSignup s) => new()
    {
        Id          = s.Id,
        ShiftId     = s.ShiftId,
        UserId      = s.UserId,
        Status      = s.Status.ToString(),
        HoursLogged = s.HoursLogged,
        SignedUpAt  = s.SignedUpAt
    };
}

public class SignUpApiRequest
{
    public Guid ShiftId { get; set; }
}
