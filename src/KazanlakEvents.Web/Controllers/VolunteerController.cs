using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.Extensions;
using KazanlakEvents.Web.Resources;
using KazanlakEvents.Web.ViewModels.Volunteer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KazanlakEvents.Web.Controllers;

public class VolunteerController(
    IVolunteerService volunteerService,
    IEventService eventService,
    ICurrentUserService currentUser,
    IUserService userService,
    INotificationService notificationService,
    IStringLocalizer<SharedResource> localizer,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> EventTasks(Guid eventId, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventByIdAsync(eventId, ct);
        if (ev == null) return NotFound();

        var tasks = await volunteerService.GetEventTasksAsync(eventId, ct);

        Guid? userId = currentUser.IsAuthenticated ? currentUser.UserId : null;

        var taskVms = tasks.Select(t => new VolunteerTaskViewModel
        {
            Id              = t.Id,
            Name            = t.Name,
            Description     = t.Description,
            VolunteersNeeded = t.VolunteersNeeded,
            Shifts          = t.Shifts.Select(s =>
            {
                var activeCount = s.Signups.Count(su =>
                    su.Status == VolunteerSignupStatus.Registered ||
                    su.Status == VolunteerSignupStatus.Confirmed);
                var userSignup = userId.HasValue
                    ? s.Signups.FirstOrDefault(su => su.UserId == userId.Value
                        && (su.Status == VolunteerSignupStatus.Registered
                            || su.Status == VolunteerSignupStatus.Confirmed))
                    : null;
                return new VolunteerShiftViewModel
                {
                    Id            = s.Id,
                    StartTime     = s.StartTime,
                    EndTime       = s.EndTime,
                    MaxVolunteers = s.MaxVolunteers,
                    SignedUpCount = activeCount,
                    IsUserSignedUp = userSignup != null,
                    UserSignupId   = userSignup?.Id
                };
            }).OrderBy(s => s.StartTime).ToList()
        }).ToList();

        return View(new EventTasksViewModel
        {
            EventId    = ev.Id,
            EventTitle = ev.Title,
            EventSlug  = ev.Slug,
            EventDate  = ev.StartDate,
            VenueName  = ev.Venue?.Name ?? string.Empty,
            Tasks      = taskVms
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(
        Guid shiftId, Guid eventId, CancellationToken ct = default)
    {
        if (!User.CanParticipate())
        {
            TempData["Error"] = localizer["AdminsCannotParticipate"].Value;
            return RedirectToAction(nameof(EventTasks), new { eventId });
        }

        var profile = await userService.GetProfileAsync(currentUser.UserId!.Value, ct);
        if (profile == null || string.IsNullOrWhiteSpace(profile.PhoneNumber))
            return RedirectToAction(nameof(PhoneRequired), new { shiftId, eventId });

        try
        {
            await volunteerService.SignUpAsync(shiftId, currentUser.UserId!.Value, ct);
            TempData["Success"] = localizer["VolunteerSignedUp"].Value;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(EventTasks), new { eventId });
    }

    [HttpGet]
    [Authorize]
    public IActionResult PhoneRequired(Guid shiftId, Guid eventId)
    {
        return View(new PhoneRequiredViewModel { ShiftId = shiftId, EventId = eventId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhoneRequired(PhoneRequiredViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        var profile = await userService.GetProfileAsync(currentUser.UserId!.Value, ct);
        if (profile != null)
        {
            profile.PhoneNumber = model.PhoneNumber.Trim();
            await userService.UpdateProfileAsync(profile, ct);
        }

        try
        {
            await volunteerService.SignUpAsync(model.ShiftId, currentUser.UserId!.Value, ct);
            TempData["Success"] = localizer["VolunteerSignedUp"].Value;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(EventTasks), new { eventId = model.EventId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSignup(
        Guid signupId, Guid eventId, CancellationToken ct = default)
    {
        try
        {
            await volunteerService.UpdateSignupStatusAsync(
                signupId, VolunteerSignupStatus.NoShow, ct);
            TempData["Success"] = localizer["VolunteerCancelled"].Value;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(EventTasks), new { eventId });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyShifts(CancellationToken ct = default)
    {
        var signups = await volunteerService.GetUserSignupsAsync(currentUser.UserId!.Value, ct);
        var (totalHours, eventCount) = await volunteerService.GetVolunteerStatsAsync(
            currentUser.UserId!.Value, ct);

        var vm = new MyShiftsViewModel
        {
            TotalHours = totalHours,
            EventCount = eventCount,
            Signups    = signups.Select(s => new VolunteerSignupViewModel
            {
                Id          = s.Id,
                ShiftId     = s.ShiftId,
                EventId     = s.Shift.Task.EventId,
                EventTitle  = s.Shift.Task.Event?.Title ?? string.Empty,
                EventSlug   = s.Shift.Task.Event?.Slug ?? string.Empty,
                TaskName    = s.Shift.Task.Name,
                ShiftStart  = s.Shift.StartTime,
                ShiftEnd    = s.Shift.EndTime,
                Status      = s.Status,
                HoursLogged = s.HoursLogged,
                SignedUpAt  = s.SignedUpAt
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    public async Task<IActionResult> ManageTasks(Guid eventId, CancellationToken ct = default)
    {
        var ev = await eventService.GetEventByIdAsync(eventId, ct);
        if (ev == null) return NotFound();

        if (ev.OrganizerId != currentUser.UserId
            && !currentUser.IsInRole(UserRoles.Admin)
            && !currentUser.IsInRole(UserRoles.SuperAdmin))
            return Forbid();

        var tasks = await volunteerService.GetEventTasksAsync(eventId, ct);

        var allUserIds = tasks
            .SelectMany(t => t.Shifts)
            .SelectMany(s => s.Signups)
            .Select(su => su.UserId)
            .Distinct()
            .ToList();

        var profileMap = new Dictionary<Guid, (string FullName, string? Phone)>();
        foreach (var uid in allUserIds)
        {
            var profile = await userService.GetProfileAsync(uid, ct);
            profileMap[uid] = profile != null
                ? (profile.FullName, profile.PhoneNumber)
                : ((await userManager.FindByIdAsync(uid.ToString()))?.UserName ?? uid.ToString(), null);
        }

        var taskVms = tasks.Select(t => new ManageTaskViewModel
        {
            Id               = t.Id,
            Name             = t.Name,
            Description      = t.Description,
            VolunteersNeeded = t.VolunteersNeeded,
            Shifts           = t.Shifts.Select(s =>
            {
                var activeSignups = s.Signups
                    .Where(su => su.Status == VolunteerSignupStatus.Registered ||
                                 su.Status == VolunteerSignupStatus.Confirmed)
                    .ToList();
                return new ManageShiftViewModel
                {
                    Id            = s.Id,
                    StartTime     = s.StartTime,
                    EndTime       = s.EndTime,
                    MaxVolunteers = s.MaxVolunteers,
                    SignedUpCount = activeSignups.Count,
                    Signups       = s.Signups.Select(su =>
                    {
                        var (name, phone) = profileMap.GetValueOrDefault(su.UserId, (su.UserId.ToString(), null));
                        return new ManageSignupViewModel
                        {
                            Id            = su.Id,
                            VolunteerName = name,
                            PhoneNumber   = phone,
                            Status        = su.Status,
                            HoursLogged   = su.HoursLogged
                        };
                    }).ToList()
                };
            }).OrderBy(s => s.StartTime).ToList()
        }).ToList();

        return View(new ManageTasksViewModel
        {
            EventId    = ev.Id,
            EventTitle = ev.Title,
            EventSlug  = ev.Slug,
            EventDate  = ev.StartDate,
            VenueName  = ev.Venue?.Name ?? string.Empty,
            Tasks      = taskVms
        });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask(
        Guid eventId, string name, string? description, int volunteersNeeded,
        CancellationToken ct = default)
    {
        await volunteerService.CreateTaskAsync(new VolunteerTask
        {
            EventId          = eventId,
            Name             = name,
            Description      = description,
            VolunteersNeeded = volunteersNeeded
        }, ct);
        TempData["Success"] = localizer["TaskCreated"].Value;
        return RedirectToAction(nameof(ManageTasks), new { eventId });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(
        Guid taskId, Guid eventId, CancellationToken ct = default)
    {
        await volunteerService.DeleteTaskAsync(taskId, ct);
        TempData["Success"] = localizer["TaskDeleted"].Value;
        return RedirectToAction(nameof(ManageTasks), new { eventId });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShift(
        Guid taskId, Guid eventId, DateTime startTime, DateTime endTime, int maxVolunteers,
        CancellationToken ct = default)
    {
        await volunteerService.CreateShiftAsync(new VolunteerShift
        {
            TaskId       = taskId,
            StartTime    = startTime,
            EndTime      = endTime,
            MaxVolunteers = maxVolunteers
        }, ct);
        TempData["Success"] = localizer["ShiftCreated"].Value;
        return RedirectToAction(nameof(ManageTasks), new { eventId });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShift(
        Guid shiftId, Guid eventId, CancellationToken ct = default)
    {
        await volunteerService.DeleteShiftAsync(shiftId, ct);
        return RedirectToAction(nameof(ManageTasks), new { eventId });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSignupStatus(
        Guid signupId, VolunteerSignupStatus status, Guid eventId, CancellationToken ct = default)
    {
        await volunteerService.UpdateSignupStatusAsync(signupId, status, ct);
        return RedirectToAction(nameof(ManageTasks), new { eventId });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHoursLogged(
        Guid signupId, decimal hours, Guid eventId, CancellationToken ct = default)
    {
        await volunteerService.LogHoursAsync(signupId, hours, ct);
        return RedirectToAction(nameof(ManageTasks), new { eventId });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveVolunteer(
        Guid signupId, string reason, string? additionalNotes, CancellationToken ct = default)
    {
        var (volunteerId, taskName, eventId) = await volunteerService.RemoveSignupAsync(signupId, ct);

        var reasonText = localizer[$"Reason{reason}"].Value;
        if (!string.IsNullOrWhiteSpace(additionalNotes))
            reasonText += ": " + additionalNotes;

        var title   = localizer["NotifVolunteerRemovedTitle"].Value;
        var message = string.Format(localizer["NotifVolunteerRemovedMsg"].Value, taskName, reasonText);

        await notificationService.SendNotificationAsync(
            volunteerId,
            NotificationType.VolunteerRemoved,
            title,
            message,
            ct: ct);

        TempData["Success"] = localizer["VolunteerRemoved"].Value;
        return RedirectToAction(nameof(ManageTasks), new { eventId });
    }
}
