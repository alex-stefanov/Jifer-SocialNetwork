using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface IAdminService
{
    Task<int> GetTotalUsersCountAsync(CancellationToken ct = default);
    Task<int> GetTotalEventsCountAsync(CancellationToken ct = default);
    Task<int> GetPendingApprovalsCountAsync(CancellationToken ct = default);
    Task<int> GetTotalTicketsSoldAsync(CancellationToken ct = default);

    Task WarnUserAsync(Guid userId, Guid issuedById, string reason, WarningType type, DateTime? expiresAt = null, CancellationToken ct = default);
    Task<IReadOnlyList<UserWarning>> GetUserWarningsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsUserBannedAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(int page, int pageSize, string? action = null, Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<int> GetAuditLogsTotalCountAsync(string? action = null, Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}
