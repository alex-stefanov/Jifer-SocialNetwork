using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Infrastructure.Repositories;

public class CommentRepository : Repository<Comment>, ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Comment>> GetByEventAsync(Guid eventId, CancellationToken ct = default)
        => await _context.Comments
            .AsNoTracking()
            .Where(c => c.EventId == eventId && c.ParentCommentId == null)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Replies)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Comment>> GetRepliesAsync(Guid parentCommentId, CancellationToken ct = default)
        => await _context.Comments
            .AsNoTracking()
            .Where(c => c.ParentCommentId == parentCommentId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> GetCountByEventAsync(Guid eventId, CancellationToken ct = default)
        => await _context.Comments
            .CountAsync(c => c.EventId == eventId, ct);
}
