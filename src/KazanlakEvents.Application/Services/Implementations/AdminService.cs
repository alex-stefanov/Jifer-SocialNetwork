using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Application.Services.Implementations;

public class AdminService(
    IApplicationDbContext db,
    IUnitOfWork unitOfWork) : IAdminService
{
    public async Task<int> GetTotalUsersCountAsync(CancellationToken ct = default)
        => await db.UserProfiles.CountAsync(ct);

    public async Task<int> GetTotalEventsCountAsync(CancellationToken ct = default)
        => await db.Events.CountAsync(e => e.Status == EventStatus.Published, ct);

    public async Task<int> GetPendingApprovalsCountAsync(CancellationToken ct = default)
        => await db.Events.CountAsync(e => e.Status == EventStatus.PendingApproval, ct);

    public async Task<int> GetTotalTicketsSoldAsync(CancellationToken ct = default)
        => await db.Tickets.CountAsync(ct);

    public async Task WarnUserAsync(
        Guid userId, Guid issuedById, string reason, WarningType type,
        DateTime? expiresAt = null, CancellationToken ct = default)
    {
        db.UserWarnings.Add(new UserWarning
        {
            UserId     = userId,
            IssuedById = issuedById,
            Reason     = reason,
            Type       = type,
            ExpiresAt  = expiresAt,
            CreatedAt  = DateTime.UtcNow
        });

        await unitOfWork.SaveChangesAsync(ct);
        // Applying Identity LockoutEnd for bans is handled by the controller via
        // UserManager<ApplicationUser> — that's an Infrastructure concern outside this service.
    }

    public async Task<IReadOnlyList<UserWarning>> GetUserWarningsAsync(
        Guid userId, CancellationToken ct = default)
        => await db.UserWarnings
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> IsUserBannedAsync(Guid userId, CancellationToken ct = default)
        => await db.UserWarnings.AnyAsync(w =>
            w.UserId == userId && (
                w.Type == WarningType.PermBan ||
                (w.Type == WarningType.TempBan && w.ExpiresAt > DateTime.UtcNow)),
            ct);

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(
        int page, int pageSize,
        string? action = null, Guid? userId = null,
        DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action.Contains(action));

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetAuditLogsTotalCountAsync(
        string? action = null, Guid? userId = null,
        DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action.Contains(action));

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        return await query.CountAsync(ct);
    }
}
