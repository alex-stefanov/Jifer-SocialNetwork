using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class CommentServiceTests
{
    private Mock<ICommentRepository>    _commentRepo = null!;
    private Mock<IEventRepository>      _eventRepo = null!;
    private Mock<IUnitOfWork>           _uow = null!;
    private Mock<ICurrentUserService>   _currentUser = null!;
    private Mock<INotificationService>  _notifications = null!;
    private Mock<IHtmlSanitizerService> _sanitizer = null!;
    private Mock<ILogger<CommentService>> _logger = null!;
    private CommentService              _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _commentRepo   = new Mock<ICommentRepository>();
        _eventRepo     = new Mock<IEventRepository>();
        _uow           = new Mock<IUnitOfWork>();
        _currentUser   = new Mock<ICurrentUserService>();
        _notifications = new Mock<INotificationService>();
        _sanitizer     = new Mock<IHtmlSanitizerService>();
        _logger        = new Mock<ILogger<CommentService>>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sanitizer.Setup(h => h.Sanitize(It.IsAny<string>())).Returns<string>(s => $"sanitized:{s}");
        _notifications.Setup(n => n.SendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new CommentService(
            _commentRepo.Object, _eventRepo.Object, _uow.Object,
            _currentUser.Object, _notifications.Object, _sanitizer.Object, _logger.Object);
    }

    // ── AddCommentAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task AddCommentAsync_CreatesCommentWithSanitizedContent()
    {
        var eventId = Guid.NewGuid();
        var userId  = Guid.NewGuid();
        Comment? captured = null;

        _commentRepo.Setup(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .Callback<Comment, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((Comment c, CancellationToken _) => c);

        var ev = new Event
        {
            Id          = eventId,
            OrganizerId = Guid.NewGuid(),
            Slug        = "ev-slug",
            Title       = "Event",
            Description = "d",
            CategoryId  = 1,
            StartDate   = DateTime.UtcNow,
            EndDate     = DateTime.UtcNow.AddHours(1)
        };
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.AddCommentAsync(eventId, userId, "Hello");

        Assert.That(captured,             Is.Not.Null);
        Assert.That(captured!.EventId,    Is.EqualTo(eventId));
        Assert.That(captured.UserId,      Is.EqualTo(userId));
        Assert.That(captured.Content,     Does.Contain("sanitized:"));
        Assert.That(captured.IsEdited,    Is.False);
        Assert.That(captured.IsHidden,    Is.False);
        Assert.That(captured.UpvoteCount, Is.EqualTo(0));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddCommentAsync_WithParentCommentId_SetsParentCommentId()
    {
        var eventId   = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var parentId  = Guid.NewGuid();
        Comment? captured = null;

        _commentRepo.Setup(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .Callback<Comment, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((Comment c, CancellationToken _) => c);
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        await _sut.AddCommentAsync(eventId, userId, "Reply", parentCommentId: parentId);

        Assert.That(captured!.ParentCommentId, Is.EqualTo(parentId));
    }

    [Test]
    public async Task AddCommentAsync_OrganizerIsCommenter_DoesNotSendNotification()
    {
        var eventId     = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        _commentRepo.Setup(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment c, CancellationToken _) => c);

        var ev = new Event
        {
            Id          = eventId,
            OrganizerId = organizerId,
            Slug        = "slug",
            Title       = "Title",
            Description = "d",
            CategoryId  = 1,
            StartDate   = DateTime.UtcNow,
            EndDate     = DateTime.UtcNow.AddHours(1)
        };
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        // userId == organizerId → no notification
        await _sut.AddCommentAsync(eventId, organizerId, "My own event comment");

        _notifications.Verify(n => n.SendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddCommentAsync_NotificationThrows_DoesNotAbortAndReturnsComment()
    {
        var eventId  = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var orgId    = Guid.NewGuid();

        _commentRepo.Setup(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment c, CancellationToken _) => c);

        var ev = new Event
        {
            Id = eventId, OrganizerId = orgId,
            Slug = "s", Title = "T", Description = "d",
            CategoryId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1)
        };
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _notifications.Setup(n => n.SendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("notification failure"));

        // Should not throw
        var result = await _sut.AddCommentAsync(eventId, userId, "Text");
        Assert.That(result, Is.Not.Null);
    }

    // ── UpdateCommentAsync ───────────────────────────────────────────────────

    [Test]
    public async Task UpdateCommentAsync_OwnerUpdates_UpdatesContentAndSetsEdited()
    {
        var userId    = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var comment   = new Comment
        {
            Id      = commentId,
            UserId  = userId,
            EventId = Guid.NewGuid(),
            Content = "old"
        };

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        var result = await _sut.UpdateCommentAsync(commentId, "new content");

        Assert.That(result.Content,  Does.Contain("sanitized:"));
        Assert.That(result.IsEdited, Is.True);
        Assert.That(result.ModifiedAt, Is.Not.Null);
        _commentRepo.Verify(r => r.Update(comment), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateCommentAsync_CommentNotFound_ThrowsNotFoundException()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateCommentAsync(Guid.NewGuid(), "text"));
    }

    [Test]
    public async Task UpdateCommentAsync_NotOwner_ThrowsForbiddenAccessException()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id      = commentId,
            UserId  = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Content = "old"
        };

        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => _sut.UpdateCommentAsync(commentId, "text"));
    }

    [Test]
    public async Task UpdateCommentAsync_NoCurrentUser_ThrowsForbiddenAccessException()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id      = commentId,
            UserId  = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Content = "old"
        };

        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => _sut.UpdateCommentAsync(commentId, "text"));
    }

    // ── DeleteCommentAsync ───────────────────────────────────────────────────

    [Test]
    public async Task DeleteCommentAsync_Owner_DeletesComment()
    {
        var userId    = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, UserId = userId, EventId = Guid.NewGuid(), Content = "c" };

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        await _sut.DeleteCommentAsync(commentId);

        _commentRepo.Verify(r => r.Remove(comment), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteCommentAsync_Moderator_CanDeleteOthersComment()
    {
        var modId     = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, UserId = Guid.NewGuid(), EventId = Guid.NewGuid(), Content = "c" };

        _currentUser.Setup(c => c.UserId).Returns(modId);
        _currentUser.Setup(c => c.IsInRole(UserRoles.Moderator)).Returns(true);
        _currentUser.Setup(c => c.IsInRole(It.Is<string>(r => r != UserRoles.Moderator))).Returns(false);
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        await _sut.DeleteCommentAsync(commentId);

        _commentRepo.Verify(r => r.Remove(comment), Times.Once);
    }

    [Test]
    public async Task DeleteCommentAsync_NotOwnerNotModerator_ThrowsForbiddenAccessException()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, UserId = Guid.NewGuid(), EventId = Guid.NewGuid(), Content = "c" };

        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => _sut.DeleteCommentAsync(commentId));
    }

    [Test]
    public async Task DeleteCommentAsync_CommentNotFound_ThrowsNotFoundException()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteCommentAsync(Guid.NewGuid()));
    }

    // ── HideCommentAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task HideCommentAsync_Admin_HidesComment()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, UserId = Guid.NewGuid(), EventId = Guid.NewGuid(), Content = "c" };

        _currentUser.Setup(c => c.IsInRole(UserRoles.Admin)).Returns(true);
        _currentUser.Setup(c => c.IsInRole(It.Is<string>(r => r != UserRoles.Admin))).Returns(false);
        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        await _sut.HideCommentAsync(commentId);

        Assert.That(comment.IsHidden, Is.True);
        Assert.That(comment.ModifiedAt, Is.Not.Null);
        _commentRepo.Verify(r => r.Update(comment), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HideCommentAsync_NotModerator_ThrowsForbiddenAccessException()
    {
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => _sut.HideCommentAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task HideCommentAsync_CommentNotFound_ThrowsNotFoundException()
    {
        _currentUser.Setup(c => c.IsInRole(UserRoles.Moderator)).Returns(true);
        _commentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.HideCommentAsync(Guid.NewGuid()));
    }

    // ── GetEventCommentsAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetEventCommentsAsync_ReturnsTopLevelCommentsWithReplies()
    {
        var eventId  = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var reply = new Comment
        {
            Id              = Guid.NewGuid(),
            EventId         = eventId,
            UserId          = Guid.NewGuid(),
            Content         = "reply",
            ParentCommentId = parentId,
            CreatedAt       = DateTime.UtcNow
        };
        var parent = new Comment
        {
            Id              = parentId,
            EventId         = eventId,
            UserId          = Guid.NewGuid(),
            Content         = "parent",
            ParentCommentId = null,
            CreatedAt       = DateTime.UtcNow.AddSeconds(-1),
            Replies         = new List<Comment> { reply }
        };
        var otherEvent = new Comment
        {
            Id              = Guid.NewGuid(),
            EventId         = Guid.NewGuid(),
            UserId          = Guid.NewGuid(),
            Content         = "other",
            ParentCommentId = null,
            CreatedAt       = DateTime.UtcNow
        };

        var all = new List<Comment> { parent, reply, otherEvent };
        _commentRepo.Setup(r => r.Query()).Returns(all.AsQueryable().BuildMock());

        var result = await _sut.GetEventCommentsAsync(eventId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(parentId));
    }

    // ── UpvoteCommentAsync ───────────────────────────────────────────────────

    [Test]
    public async Task UpvoteCommentAsync_IncrementsUpvoteCount()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id          = commentId,
            UserId      = Guid.NewGuid(),
            EventId     = Guid.NewGuid(),
            Content     = "c",
            UpvoteCount = 3
        };

        _commentRepo.Setup(r => r.GetByIdAsync(commentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        await _sut.UpvoteCommentAsync(commentId);

        Assert.That(comment.UpvoteCount, Is.EqualTo(4));
        _commentRepo.Verify(r => r.Update(comment), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpvoteCommentAsync_CommentNotFound_ThrowsNotFoundException()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.UpvoteCommentAsync(Guid.NewGuid()));
    }
}
