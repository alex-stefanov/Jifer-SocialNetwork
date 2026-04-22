using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface ICommentService
{
    Task<Comment> AddCommentAsync(Guid eventId, Guid userId, string content, Guid? parentCommentId = null, CancellationToken ct = default);
    Task<Comment> UpdateCommentAsync(Guid commentId, string content, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default);
    Task HideCommentAsync(Guid commentId, CancellationToken ct = default);
    Task<IReadOnlyList<Comment>> GetEventCommentsAsync(Guid eventId, CancellationToken ct = default);
    Task UpvoteCommentAsync(Guid commentId, CancellationToken ct = default);
}
