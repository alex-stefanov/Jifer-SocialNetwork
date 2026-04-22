using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<IReadOnlyList<Comment>> GetByEventAsync(Guid eventId, CancellationToken ct = default);
    Task<IReadOnlyList<Comment>> GetRepliesAsync(Guid parentCommentId, CancellationToken ct = default);
    Task<int> GetCountByEventAsync(Guid eventId, CancellationToken ct = default);
}
