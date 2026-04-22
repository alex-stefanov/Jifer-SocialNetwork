using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface IVolunteerService
{
    Task<VolunteerTask> CreateTaskAsync(VolunteerTask task, CancellationToken ct = default);
    Task<VolunteerShift> CreateShiftAsync(VolunteerShift shift, CancellationToken ct = default);
    Task<VolunteerSignup> SignUpAsync(Guid shiftId, Guid userId, CancellationToken ct = default);
    Task<VolunteerSignup> UpdateSignupStatusAsync(Guid signupId, VolunteerSignupStatus status, CancellationToken ct = default);
    Task LogHoursAsync(Guid signupId, decimal hours, CancellationToken ct = default);
    Task<IReadOnlyList<VolunteerTask>> GetEventTasksAsync(Guid eventId, CancellationToken ct = default);
    Task<IReadOnlyList<VolunteerSignup>> GetUserSignupsAsync(Guid userId, CancellationToken ct = default);
    Task<(int TotalHours, int EventCount)> GetVolunteerStatsAsync(Guid userId, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default);
    Task DeleteShiftAsync(Guid shiftId, CancellationToken ct = default);
    Task<(Guid VolunteerUserId, string TaskName, Guid EventId)> RemoveSignupAsync(Guid signupId, CancellationToken ct = default);
}
