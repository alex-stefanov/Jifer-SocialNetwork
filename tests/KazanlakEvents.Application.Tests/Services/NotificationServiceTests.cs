using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<INotificationRepository>    _notifRepo = null!;
    private Mock<IUnitOfWork>                _uow = null!;
    private Mock<ILogger<NotificationService>> _logger = null!;
    private NotificationService              _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _notifRepo = new Mock<INotificationRepository>();
        _uow       = new Mock<IUnitOfWork>();
        _logger    = new Mock<ILogger<NotificationService>>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new NotificationService(_notifRepo.Object, _uow.Object, _logger.Object);
    }

    // ── SendNotificationAsync ────────────────────────────────────────────────

    [Test]
    public async Task SendNotificationAsync_CreatesAndSavesNotification()
    {
        Notification? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => captured = n)
            .ReturnsAsync((Notification n, CancellationToken _) => n);

        var userId = Guid.NewGuid();
        await _sut.SendNotificationAsync(
            userId, NotificationType.EventApproved, "Approved", "Your event was approved!", "/events");

        Assert.That(captured,            Is.Not.Null);
        Assert.That(captured!.UserId,    Is.EqualTo(userId));
        Assert.That(captured.Type,       Is.EqualTo(NotificationType.EventApproved));
        Assert.That(captured.Title,      Is.EqualTo("Approved"));
        Assert.That(captured.Message,    Is.EqualTo("Your event was approved!"));
        Assert.That(captured.LinkUrl,    Is.EqualTo("/events"));
        Assert.That(captured.IsRead,     Is.False);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendNotificationAsync_NullLinkUrl_StillSaves()
    {
        Notification? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => captured = n)
            .ReturnsAsync((Notification n, CancellationToken _) => n);

        await _sut.SendNotificationAsync(
            Guid.NewGuid(), NotificationType.NewFollower, "Follower", "Someone followed you");

        Assert.That(captured!.LinkUrl, Is.Null);
    }

    // ── SendBulkNotificationAsync ────────────────────────────────────────────

    [Test]
    public async Task SendBulkNotificationAsync_CreatesOneNotificationPerUser()
    {
        IEnumerable<Notification>? captured = null;
        _notifRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Notification>, CancellationToken>((notifs, _) => captured = notifs)
            .Returns(Task.CompletedTask);

        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        await _sut.SendBulkNotificationAsync(userIds, NotificationType.EventCancelled, "Cancelled", "Event was cancelled");

        Assert.That(captured,                        Is.Not.Null);
        Assert.That(captured!.Count(),               Is.EqualTo(3));
        Assert.That(captured.All(n => !n.IsRead),    Is.True);
        Assert.That(captured.Select(n => n.UserId).Distinct().Count(), Is.EqualTo(3));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendBulkNotificationAsync_EmptyList_SavesNothingEffectively()
    {
        _notifRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.SendBulkNotificationAsync(
            Array.Empty<Guid>(), NotificationType.EventCancelled, "Title", "Msg");

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetUserNotificationsAsync ────────────────────────────────────────────

    [Test]
    public async Task GetUserNotificationsAsync_NoFilter_CallsRepositoryWithNullTypes()
    {
        var userId = Guid.NewGuid();
        _notifRepo.Setup(r => r.GetByUserAsync(
            userId, 1, 20, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>().AsReadOnly());

        await _sut.GetUserNotificationsAsync(userId, 1, 20, filter: null);

        _notifRepo.Verify(r => r.GetByUserAsync(userId, 1, 20, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetUserNotificationsAsync_UnreadFilter_PassesUnreadOnly()
    {
        var userId = Guid.NewGuid();
        _notifRepo.Setup(r => r.GetByUserAsync(
            userId, 1, 20, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>().AsReadOnly());

        await _sut.GetUserNotificationsAsync(userId, 1, 20, filter: "unread");

        _notifRepo.Verify(r => r.GetByUserAsync(userId, 1, 20, null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetUserNotificationsAsync_EventsFilter_PassesEventTypes()
    {
        var userId = Guid.NewGuid();
        NotificationType[]? capturedTypes = null;
        _notifRepo.Setup(r => r.GetByUserAsync(
            userId, 1, 20, It.IsAny<IEnumerable<NotificationType>?>(), false, It.IsAny<CancellationToken>()))
            .Callback<Guid, int, int, IEnumerable<NotificationType>?, bool, CancellationToken>(
                (_, _, _, types, _, _) => capturedTypes = types?.ToArray())
            .ReturnsAsync(new List<Notification>().AsReadOnly());

        await _sut.GetUserNotificationsAsync(userId, 1, 20, filter: "events");

        Assert.That(capturedTypes, Is.Not.Null);
        Assert.That(capturedTypes, Contains.Item(NotificationType.EventApproved));
        Assert.That(capturedTypes, Contains.Item(NotificationType.EventRejected));
        Assert.That(capturedTypes, Contains.Item(NotificationType.EventCancelled));
    }

    [Test]
    public async Task GetUserNotificationsAsync_TicketsFilter_PassesTicketTypes()
    {
        var userId = Guid.NewGuid();
        NotificationType[]? capturedTypes = null;
        _notifRepo.Setup(r => r.GetByUserAsync(
            userId, 1, 20, It.IsAny<IEnumerable<NotificationType>?>(), false, It.IsAny<CancellationToken>()))
            .Callback<Guid, int, int, IEnumerable<NotificationType>?, bool, CancellationToken>(
                (_, _, _, types, _, _) => capturedTypes = types?.ToArray())
            .ReturnsAsync(new List<Notification>().AsReadOnly());

        await _sut.GetUserNotificationsAsync(userId, 1, 20, filter: "tickets");

        Assert.That(capturedTypes, Contains.Item(NotificationType.TicketPurchased));
    }

    [Test]
    public async Task GetUserNotificationsAsync_SocialFilter_PassesSocialTypes()
    {
        var userId = Guid.NewGuid();
        NotificationType[]? capturedTypes = null;
        _notifRepo.Setup(r => r.GetByUserAsync(
            userId, 1, 20, It.IsAny<IEnumerable<NotificationType>?>(), false, It.IsAny<CancellationToken>()))
            .Callback<Guid, int, int, IEnumerable<NotificationType>?, bool, CancellationToken>(
                (_, _, _, types, _, _) => capturedTypes = types?.ToArray())
            .ReturnsAsync(new List<Notification>().AsReadOnly());

        await _sut.GetUserNotificationsAsync(userId, 1, 20, filter: "social");

        Assert.That(capturedTypes, Contains.Item(NotificationType.NewFollower));
        Assert.That(capturedTypes, Contains.Item(NotificationType.NewComment));
        Assert.That(capturedTypes, Contains.Item(NotificationType.NewRating));
    }

    // ── GetUnreadCountAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetUnreadCountAsync_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        _notifRepo.Setup(r => r.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await _sut.GetUnreadCountAsync(userId);

        Assert.That(result, Is.EqualTo(7));
    }

    // ── GetTotalCountAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetTotalCountAsync_NoFilter_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        _notifRepo.Setup(r => r.GetTotalCountAsync(userId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        var result = await _sut.GetTotalCountAsync(userId, filter: null);

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public async Task GetTotalCountAsync_UnreadFilter_PassesUnreadOnlyTrue()
    {
        var userId = Guid.NewGuid();
        _notifRepo.Setup(r => r.GetTotalCountAsync(userId, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _sut.GetTotalCountAsync(userId, filter: "unread");

        Assert.That(result, Is.EqualTo(3));
        _notifRepo.Verify(r => r.GetTotalCountAsync(userId, null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── MarkAsReadAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task MarkAsReadAsync_ExistingNotification_MarksAsReadAndSaves()
    {
        var notifId = Guid.NewGuid();
        var notif = new Notification
        {
            Id     = notifId,
            UserId = Guid.NewGuid(),
            Type   = NotificationType.NewComment,
            Title  = "T",
            Message = "M",
            IsRead = false
        };

        _notifRepo.Setup(r => r.GetByIdAsync(notifId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notif);

        await _sut.MarkAsReadAsync(notifId);

        Assert.That(notif.IsRead, Is.True);
        _notifRepo.Verify(r => r.Update(notif), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MarkAsReadAsync_NotificationNotFound_ThrowsNotFoundException()
    {
        _notifRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.MarkAsReadAsync(Guid.NewGuid()));
    }

    // ── MarkAllAsReadAsync ───────────────────────────────────────────────────

    [Test]
    public async Task MarkAllAsReadAsync_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        _notifRepo.Setup(r => r.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.MarkAllAsReadAsync(userId);

        _notifRepo.Verify(r => r.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
