using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Services.Implementations;

public class CommentService(
    ICommentRepository commentRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    IHtmlSanitizerService htmlSanitizer,
    ILogger<CommentService> logger) : ICommentService
{
    public async Task<Comment> AddCommentAsync(
        Guid eventId, Guid userId, string content,
        Guid? parentCommentId = null, CancellationToken ct = default)
    {
        var comment = new Comment
        {
            EventId         = eventId,
            UserId          = userId,
            Content         = htmlSanitizer.Sanitize(content),
            ParentCommentId = parentCommentId,
            IsEdited        = false,
            IsHidden        = false,
            UpvoteCount     = 0,
            CreatedAt       = DateTime.UtcNow
        };

        await commentRepository.AddAsync(comment, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Comment {Id} added to Event {EventId} by User {UserId}", comment.Id, eventId, userId);

        // Notification is fire-and-forget; a failure must not abort the comment save.
        try
        {
            var ev = await eventRepository.GetByIdAsync(eventId, ct);
            if (ev != null && ev.OrganizerId != userId)
            {
                await notificationService.SendNotificationAsync(
                    ev.OrganizerId,
                    NotificationType.NewComment,
                    "New comment",
                    $"A new comment was posted on your event '{ev.Title}'.",
                    linkUrl: $"/Event/Details/{ev.Slug}",
                    ct: ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send new-comment notification for event {EventId}", eventId);
        }

        return comment;
    }

    public async Task<Comment> UpdateCommentAsync(
        Guid commentId, string content, CancellationToken ct = default)
    {
        var comment = await commentRepository.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException(nameof(Comment), commentId);

        if (currentUser.UserId == null || comment.UserId != currentUser.UserId.Value)
            throw new ForbiddenAccessException();

        comment.Content   = htmlSanitizer.Sanitize(content);
        comment.IsEdited  = true;
        comment.ModifiedAt = DateTime.UtcNow;

        commentRepository.Update(comment);
        await unitOfWork.SaveChangesAsync(ct);
        return comment;
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        var comment = await commentRepository.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException(nameof(Comment), commentId);

        bool isOwner = comment.UserId == currentUser.UserId;
        bool isMod   = currentUser.IsInRole(UserRoles.Moderator)
                    || currentUser.IsInRole(UserRoles.Admin)
                    || currentUser.IsInRole(UserRoles.SuperAdmin);

        if (!isOwner && !isMod)
            throw new ForbiddenAccessException();

        commentRepository.Remove(comment);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Comment {Id} deleted", commentId);
    }

    public async Task HideCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRoles.Moderator)
            && !currentUser.IsInRole(UserRoles.Admin)
            && !currentUser.IsInRole(UserRoles.SuperAdmin))
            throw new ForbiddenAccessException();

        var comment = await commentRepository.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException(nameof(Comment), commentId);

        comment.IsHidden  = true;
        comment.ModifiedAt = DateTime.UtcNow;

        commentRepository.Update(comment);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Comment {Id} hidden by {UserId}", commentId, currentUser.UserId);
    }

    public async Task<IReadOnlyList<Comment>> GetEventCommentsAsync(
        Guid eventId, CancellationToken ct = default)
    {
        return await commentRepository.Query()
            .Where(c => c.EventId == eventId && c.ParentCommentId == null)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Replies)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task UpvoteCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        var comment = await commentRepository.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException(nameof(Comment), commentId);

        comment.UpvoteCount++;
        commentRepository.Update(comment);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
