using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Application.Services.Implementations;

public class VolunteerService(
    IRepository<VolunteerTask> taskRepo,
    IRepository<VolunteerShift> shiftRepo,
    IRepository<VolunteerSignup> signupRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    INotificationService notifications) : IVolunteerService
{
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<VolunteerTask> CreateTaskAsync(VolunteerTask task, CancellationToken ct = default)
    {
        var created = await taskRepo.AddAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return created;
    }

    public async Task<VolunteerShift> CreateShiftAsync(VolunteerShift shift, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAsync(shift.TaskId, ct)
            ?? throw new NotFoundException(nameof(VolunteerTask), shift.TaskId);

        var created = await shiftRepo.AddAsync(shift, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return created;
    }

    public async Task<VolunteerSignup> SignUpAsync(
        Guid shiftId, Guid userId, CancellationToken ct = default)
    {
        var shift = await shiftRepo.Query()
            .Where(s => s.Id == shiftId)
            .Include(s => s.Signups)
            .Include(s => s.Task).ThenInclude(t => t.Event)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(VolunteerShift), shiftId);

        if (shift.Signups.Any(s => s.UserId == userId))
            throw new InvalidOperationException("You are already signed up for this shift.");

        var activeSignups = shift.Signups.Count(s =>
            s.Status == VolunteerSignupStatus.Registered ||
            s.Status == VolunteerSignupStatus.Confirmed);

        if (activeSignups >= shift.MaxVolunteers)
            throw new InvalidOperationException("This shift is already full.");

        var signup = await signupRepo.AddAsync(new VolunteerSignup
        {
            ShiftId    = shiftId,
            UserId     = userId,
            Status     = VolunteerSignupStatus.Registered,
            SignedUpAt = DateTime.UtcNow
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);

        var organizerId = shift.Task.Event.OrganizerId;
        await notifications.SendNotificationAsync(
            organizerId,
            NotificationType.VolunteerSignup,
            "New volunteer",
            $"A volunteer signed up for task \"{shift.Task.Name}\".",
            linkUrl: $"/Event/Details/{shift.Task.Event.Slug}",
            ct: ct);

        return signup;
    }

    public async Task<VolunteerSignup> UpdateSignupStatusAsync(
        Guid signupId, VolunteerSignupStatus status, CancellationToken ct = default)
    {
        var signup = await signupRepo.GetByIdAsync(signupId, ct)
            ?? throw new NotFoundException(nameof(VolunteerSignup), signupId);

        signup.Status = status;
        signupRepo.Update(signup);
        await unitOfWork.SaveChangesAsync(ct);
        return signup;
    }

    public async Task LogHoursAsync(Guid signupId, decimal hours, CancellationToken ct = default)
    {
        var signup = await signupRepo.GetByIdAsync(signupId, ct)
            ?? throw new NotFoundException(nameof(VolunteerSignup), signupId);

        signup.HoursLogged = hours;
        signupRepo.Update(signup);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<VolunteerTask>> GetEventTasksAsync(
        Guid eventId, CancellationToken ct = default)
        => await taskRepo.Query()
            .Where(t => t.EventId == eventId)
            .Include(t => t.Shifts)
                .ThenInclude(s => s.Signups)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<VolunteerSignup>> GetUserSignupsAsync(
        Guid userId, CancellationToken ct = default)
        => await signupRepo.Query()
            .Where(s => s.UserId == userId)
            .Include(s => s.Shift)
                .ThenInclude(sh => sh.Task)
                    .ThenInclude(t => t.Event)
            .OrderByDescending(s => s.SignedUpAt)
            .ToListAsync(ct);

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException(nameof(VolunteerTask), taskId);
        taskRepo.Remove(task);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteShiftAsync(Guid shiftId, CancellationToken ct = default)
    {
        var shift = await shiftRepo.GetByIdAsync(shiftId, ct)
            ?? throw new NotFoundException(nameof(VolunteerShift), shiftId);
        shiftRepo.Remove(shift);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<(Guid VolunteerUserId, string TaskName, Guid EventId)> RemoveSignupAsync(
        Guid signupId, CancellationToken ct = default)
    {
        var signup = await signupRepo.Query()
            .Where(s => s.Id == signupId)
            .Include(s => s.Shift).ThenInclude(sh => sh.Task)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(VolunteerSignup), signupId);

        var volunteerId = signup.UserId;
        var taskName    = signup.Shift.Task.Name;
        var eventId     = signup.Shift.Task.EventId;

        signupRepo.Remove(signup);
        await unitOfWork.SaveChangesAsync(ct);

        return (volunteerId, taskName, eventId);
    }

    public async Task<(int TotalHours, int EventCount)> GetVolunteerStatsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var completed = await signupRepo.Query()
            .Where(s => s.UserId == userId && s.Status == VolunteerSignupStatus.Completed)
            .Include(s => s.Shift).ThenInclude(sh => sh.Task)
            .ToListAsync(ct);

        var totalHours = (int)(completed.Sum(s => s.HoursLogged ?? 0));
        var eventCount = completed.Select(s => s.Shift.Task.EventId).Distinct().Count();
        return (totalHours, eventCount);
    }
}
