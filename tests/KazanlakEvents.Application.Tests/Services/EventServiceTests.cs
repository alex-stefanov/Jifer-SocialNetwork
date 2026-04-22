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
public class EventServiceTests
{
    private Mock<IEventRepository>      _eventRepo = null!;
    private Mock<IRepository<EventSeries>> _seriesRepo = null!;
    private Mock<IUnitOfWork>           _uow = null!;
    private Mock<ISlugService>          _slug = null!;
    private Mock<ICurrentUserService>   _currentUser = null!;
    private Mock<INotificationService>  _notifications = null!;
    private Mock<IWebhookService>       _webhooks = null!;
    private Mock<IHtmlSanitizerService> _sanitizer = null!;
    private Mock<ILogger<EventService>> _logger = null!;
    private EventService                _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _eventRepo     = new Mock<IEventRepository>();
        _seriesRepo    = new Mock<IRepository<EventSeries>>();
        _uow           = new Mock<IUnitOfWork>();
        _slug          = new Mock<ISlugService>();
        _currentUser   = new Mock<ICurrentUserService>();
        _notifications = new Mock<INotificationService>();
        _webhooks      = new Mock<IWebhookService>();
        _sanitizer     = new Mock<IHtmlSanitizerService>();
        _logger        = new Mock<ILogger<EventService>>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sanitizer.Setup(h => h.Sanitize(It.IsAny<string>())).Returns<string>(s => s);
        _slug.Setup(s => s.GenerateUniqueSlugAsync<Event>(
            It.IsAny<string>(), It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-slug");
        _webhooks.Setup(w => w.DispatchAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifications.Setup(n => n.SendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifications.Setup(n => n.SendBulkNotificationAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new EventService(
            _eventRepo.Object, _seriesRepo.Object, _uow.Object,
            _slug.Object, _currentUser.Object, _notifications.Object,
            _webhooks.Object, _sanitizer.Object, _logger.Object);
    }

    private static Event MakeEvent(
        EventStatus status = EventStatus.Draft,
        Guid? organizerId = null,
        int categoryId = 1) => new()
    {
        Id          = Guid.NewGuid(),
        Title       = "Test Event",
        Slug        = "test-event",
        Description = "Description",
        CategoryId  = categoryId,
        OrganizerId = organizerId ?? Guid.NewGuid(),
        Status      = status,
        StartDate   = DateTime.UtcNow.AddDays(1),
        EndDate     = DateTime.UtcNow.AddDays(2),
        EventTags   = new List<EventTag>()
    };

    // ── CreateEventAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task CreateEventAsync_ValidUser_CreatesEventWithSlugAndTags()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _eventRepo.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event e, CancellationToken _) => e);

        var ev = MakeEvent();
        var result = await _sut.CreateEventAsync(ev, tagIds: new[] { 1, 2, 2 });

        Assert.That(result.OrganizerId, Is.EqualTo(userId));
        Assert.That(result.Status,      Is.EqualTo(EventStatus.Draft));
        Assert.That(result.ViewCount,   Is.EqualTo(0));
        Assert.That(result.Slug,        Is.EqualTo("test-slug"));
        Assert.That(result.EventTags,   Has.Count.EqualTo(2));
        _webhooks.Verify(w => w.DispatchAsync("event.created", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateEventAsync_NoCurrentUser_ThrowsForbiddenAccessException()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => _sut.CreateEventAsync(MakeEvent()));
    }

    [Test]
    public async Task CreateEventAsync_NoTagIds_CreatesEventWithNoTags()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _eventRepo.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event e, CancellationToken _) => e);

        var ev = MakeEvent();
        var result = await _sut.CreateEventAsync(ev, tagIds: null);

        Assert.That(result.EventTags, Is.Empty);
    }

    // ── UpdateEventAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task UpdateEventAsync_Owner_UpdatesEvent()
    {
        var userId = Guid.NewGuid();
        var existing = MakeEvent(organizerId: userId);

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _eventRepo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var update = new Event
        {
            Id               = existing.Id,
            Title            = "Updated Title",
            Description      = "Updated Desc",
            ShortDescription = "Short",
            CategoryId       = 2,
            StartDate        = DateTime.UtcNow.AddDays(3),
            EndDate          = DateTime.UtcNow.AddDays(4),
            Capacity         = 100,
            IsFree           = true,
            IsAccessible     = true,
            MinAge           = 18,
            EventTags        = new List<EventTag>()
        };

        var result = await _sut.UpdateEventAsync(update);

        Assert.That(result.Title,       Is.EqualTo("Updated Title"));
        Assert.That(result.CategoryId,  Is.EqualTo(2));
        Assert.That(result.IsFree,      Is.True);
        _eventRepo.Verify(r => r.Update(existing), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateEventAsync_TitleChanged_RegeneratesSlug()
    {
        var userId = Guid.NewGuid();
        var existing = MakeEvent(organizerId: userId);
        existing.Title = "Old Title";

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _eventRepo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _slug.Setup(s => s.GenerateUniqueSlugAsync<Event>(
            "New Title", It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-title");

        var update = new Event { Id = existing.Id, Title = "New Title", Description = "d", EventTags = new List<EventTag>() };
        var result = await _sut.UpdateEventAsync(update);

        Assert.That(result.Slug, Is.EqualTo("new-title"));
    }

    [Test]
    public async Task UpdateEventAsync_NotOwnerNotAdmin_ThrowsForbiddenAccessException()
    {
        var existing = MakeEvent();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _eventRepo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Assert.ThrowsAsync<ForbiddenAccessException>(
            () => _sut.UpdateEventAsync(new Event { Id = existing.Id }));
    }

    [Test]
    public async Task UpdateEventAsync_Admin_CanUpdateOthersEvent()
    {
        var existing = MakeEvent();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(c => c.IsInRole(UserRoles.Admin)).Returns(true);
        _eventRepo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var update = new Event { Id = existing.Id, Title = existing.Title, Description = "d", EventTags = new List<EventTag>() };
        Assert.DoesNotThrowAsync(() => _sut.UpdateEventAsync(update));
    }

    [Test]
    public async Task UpdateEventAsync_EventNotFound_ThrowsNotFoundException()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateEventAsync(new Event { Id = Guid.NewGuid() }));
    }

    // ── DeleteEventAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task DeleteEventAsync_Owner_SoftDeletesEvent()
    {
        var userId = Guid.NewGuid();
        var ev = MakeEvent(organizerId: userId);

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.UserName).Returns("user1");
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        await _sut.DeleteEventAsync(ev.Id);

        Assert.That(ev.IsDeleted, Is.True);
        Assert.That(ev.DeletedAt, Is.Not.Null);
        Assert.That(ev.DeletedBy, Is.EqualTo("user1"));
        _eventRepo.Verify(r => r.Update(ev), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteEventAsync_EventNotFound_ThrowsNotFoundException()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteEventAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task DeleteEventAsync_NotOwnerNotAdmin_ThrowsForbiddenAccessException()
    {
        var ev = MakeEvent();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => _sut.DeleteEventAsync(ev.Id));
    }

    // ── GetEventByIdAsync / GetEventBySlugAsync ──────────────────────────────

    [Test]
    public async Task GetEventByIdAsync_DelegatesToRepository()
    {
        var ev = MakeEvent();
        _eventRepo.Setup(r => r.GetWithDetailsAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.GetEventByIdAsync(ev.Id);

        Assert.That(result, Is.EqualTo(ev));
    }

    [Test]
    public async Task GetEventBySlugAsync_DelegatesToRepository()
    {
        var ev = MakeEvent();
        ev.Slug = "my-slug";
        _eventRepo.Setup(r => r.GetBySlugAsync("my-slug", It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.GetEventBySlugAsync("my-slug");

        Assert.That(result, Is.EqualTo(ev));
    }

    // ── GetEventsPagedAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetEventsPagedAsync_DefaultParameters_ReturnsPublishedEventsPaged()
    {
        var events = new List<Event>
        {
            MakeEvent(EventStatus.Published, categoryId: 1),
            MakeEvent(EventStatus.Published, categoryId: 2),
            MakeEvent(EventStatus.Draft)
        };
        events[0].StartDate = DateTime.UtcNow.AddDays(1);
        events[1].StartDate = DateTime.UtcNow.AddDays(2);
        events[2].StartDate = DateTime.UtcNow.AddDays(3);

        _eventRepo.Setup(r => r.Query()).Returns(events.AsQueryable().BuildMock());

        var (items, totalCount) = await _sut.GetEventsPagedAsync(page: 1, pageSize: 10);

        Assert.That(totalCount, Is.EqualTo(2));
        Assert.That(items,      Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetEventsPagedAsync_WithCategoryFilter_FiltersCorrectly()
    {
        var events = new List<Event>
        {
            MakeEvent(EventStatus.Published, categoryId: 1),
            MakeEvent(EventStatus.Published, categoryId: 2),
            MakeEvent(EventStatus.Published, categoryId: 1)
        };
        events.ForEach(e => e.StartDate = DateTime.UtcNow.AddDays(1));
        _eventRepo.Setup(r => r.Query()).Returns(events.AsQueryable().BuildMock());

        var (items, totalCount) = await _sut.GetEventsPagedAsync(1, 10, categoryId: 1);

        Assert.That(totalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetEventsPagedAsync_WithIsFreeFilter_FiltersCorrectly()
    {
        var e1 = MakeEvent(EventStatus.Published); e1.IsFree = true;  e1.StartDate = DateTime.UtcNow.AddDays(1);
        var e2 = MakeEvent(EventStatus.Published); e2.IsFree = false; e2.StartDate = DateTime.UtcNow.AddDays(1);
        var e3 = MakeEvent(EventStatus.Published); e3.IsFree = true;  e3.StartDate = DateTime.UtcNow.AddDays(1);
        var events = new List<Event> { e1, e2, e3 };
        _eventRepo.Setup(r => r.Query()).Returns(events.AsQueryable().BuildMock());

        var (items, total) = await _sut.GetEventsPagedAsync(1, 10, isFree: true);

        Assert.That(total, Is.EqualTo(2));
    }

    [Test]
    public async Task GetEventsPagedAsync_WithFromDateFilter_FiltersCorrectly()
    {
        var fromDate = DateTime.UtcNow.AddDays(5);
        var e1 = MakeEvent(EventStatus.Published); e1.StartDate = DateTime.UtcNow.AddDays(3); e1.EndDate = DateTime.UtcNow.AddDays(4);
        var e2 = MakeEvent(EventStatus.Published); e2.StartDate = DateTime.UtcNow.AddDays(6); e2.EndDate = DateTime.UtcNow.AddDays(7);
        var events = new List<Event> { e1, e2 };
        _eventRepo.Setup(r => r.Query()).Returns(events.AsQueryable().BuildMock());

        var (items, total) = await _sut.GetEventsPagedAsync(1, 10, fromDate: fromDate);

        Assert.That(total, Is.EqualTo(1));
    }

    [Test]
    public async Task GetEventsPagedAsync_WithVolunteerTasksFilter_FiltersCorrectly()
    {
        var vtask = new VolunteerTask { Id = Guid.NewGuid(), EventId = Guid.NewGuid(), Name = "Task", VolunteersNeeded = 1 };
        var e1 = MakeEvent(EventStatus.Published); e1.StartDate = DateTime.UtcNow.AddDays(1); e1.VolunteerTasks = new List<VolunteerTask> { vtask };
        var e2 = MakeEvent(EventStatus.Published); e2.StartDate = DateTime.UtcNow.AddDays(1); e2.VolunteerTasks = new List<VolunteerTask>();
        var events = new List<Event> { e1, e2 };
        _eventRepo.Setup(r => r.Query()).Returns(events.AsQueryable().BuildMock());

        var (items, total) = await _sut.GetEventsPagedAsync(1, 10, hasVolunteerTasks: true);

        Assert.That(total, Is.EqualTo(1));
    }

    // ── GetUpcomingEventsAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetUpcomingEventsAsync_DelegatesToRepository()
    {
        var upcoming = new List<Event> { MakeEvent(EventStatus.Published) };
        _eventRepo.Setup(r => r.GetUpcomingEventsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(upcoming.AsReadOnly());

        var result = await _sut.GetUpcomingEventsAsync(5);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    // ── SubmitForApprovalAsync ───────────────────────────────────────────────

    [Test]
    public async Task SubmitForApprovalAsync_DraftEvent_ChangeStatusToPendingApproval()
    {
        var ev = MakeEvent(EventStatus.Draft);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.SubmitForApprovalAsync(ev.Id);

        Assert.That(result.Status, Is.EqualTo(EventStatus.PendingApproval));
        _eventRepo.Verify(r => r.Update(ev), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SubmitForApprovalAsync_NonDraftEvent_ThrowsInvalidOperationException()
    {
        var ev = MakeEvent(EventStatus.Published);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SubmitForApprovalAsync(ev.Id));
    }

    [Test]
    public async Task SubmitForApprovalAsync_EventNotFound_ThrowsNotFoundException()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.SubmitForApprovalAsync(Guid.NewGuid()));
    }

    // ── ApproveEventAsync ────────────────────────────────────────────────────

    [Test]
    public async Task ApproveEventAsync_PendingApprovalEvent_ApprovesAndNotifies()
    {
        var approvedById = Guid.NewGuid();
        var ev = MakeEvent(EventStatus.PendingApproval);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.ApproveEventAsync(ev.Id, approvedById);

        Assert.That(result.Status,      Is.EqualTo(EventStatus.Approved));
        Assert.That(result.ApprovedById, Is.EqualTo(approvedById));
        Assert.That(result.ApprovedAt,  Is.Not.Null);
        _webhooks.Verify(w => w.DispatchAsync("event.approved", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.SendNotificationAsync(
            ev.OrganizerId, NotificationType.EventApproved,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ApproveEventAsync_NonPendingEvent_ThrowsInvalidOperationException()
    {
        var ev = MakeEvent(EventStatus.Draft);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ApproveEventAsync(ev.Id, Guid.NewGuid()));
    }

    [Test]
    public async Task ApproveEventAsync_EventNotFound_ThrowsNotFoundException()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.ApproveEventAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── RejectEventAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task RejectEventAsync_PendingApprovalEvent_RejectsAndNotifies()
    {
        var ev = MakeEvent(EventStatus.PendingApproval);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.RejectEventAsync(ev.Id, Guid.NewGuid(), "Policy violation");

        Assert.That(result.Status,          Is.EqualTo(EventStatus.Rejected));
        Assert.That(result.RejectionReason, Is.EqualTo("Policy violation"));
        _notifications.Verify(n => n.SendNotificationAsync(
            ev.OrganizerId, NotificationType.EventRejected,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RejectEventAsync_NonPendingEvent_ThrowsInvalidOperationException()
    {
        var ev = MakeEvent(EventStatus.Approved);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RejectEventAsync(ev.Id, Guid.NewGuid(), "reason"));
    }

    // ── PublishEventAsync ────────────────────────────────────────────────────

    [Test]
    public async Task PublishEventAsync_ApprovedEvent_PublishesAndDispatchesWebhook()
    {
        var ev = MakeEvent(EventStatus.Approved);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.PublishEventAsync(ev.Id);

        Assert.That(result.Status, Is.EqualTo(EventStatus.Published));
        _webhooks.Verify(w => w.DispatchAsync("event.published", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task PublishEventAsync_NonApprovedEvent_ThrowsInvalidOperationException()
    {
        var ev = MakeEvent(EventStatus.Draft);
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PublishEventAsync(ev.Id));
    }

    // ── CancelEventAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task CancelEventAsync_PublishedEventWithAttendees_CancelsAndNotifiesAll()
    {
        var ev = MakeEvent(EventStatus.Published);
        ev.Attendances = new List<EventAttendance>
        {
            new() { UserId = Guid.NewGuid(), EventId = ev.Id, Status = AttendanceStatus.Going },
            new() { UserId = Guid.NewGuid(), EventId = ev.Id, Status = AttendanceStatus.Going }
        };
        _eventRepo.Setup(r => r.GetWithDetailsAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.CancelEventAsync(ev.Id, "Venue closed");

        Assert.That(result.Status,          Is.EqualTo(EventStatus.Cancelled));
        Assert.That(result.RejectionReason, Is.EqualTo("Venue closed"));
        _notifications.Verify(n => n.SendBulkNotificationAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Count() == 2),
            NotificationType.EventCancelled, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _webhooks.Verify(w => w.DispatchAsync("event.cancelled", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelEventAsync_NoAttendees_DoesNotSendBulkNotification()
    {
        var ev = MakeEvent(EventStatus.Published);
        ev.Attendances = new List<EventAttendance>();
        _eventRepo.Setup(r => r.GetWithDetailsAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        await _sut.CancelEventAsync(ev.Id);

        _notifications.Verify(n => n.SendBulkNotificationAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CancelEventAsync_EventNotFound_ThrowsNotFoundException()
    {
        _eventRepo.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.CancelEventAsync(Guid.NewGuid()));
    }

    // ── IncrementViewCountAsync ──────────────────────────────────────────────

    [Test]
    public async Task IncrementViewCountAsync_EventExists_IncrementsCount()
    {
        var ev = MakeEvent();
        ev.ViewCount = 5;
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        await _sut.IncrementViewCountAsync(ev.Id);

        Assert.That(ev.ViewCount, Is.EqualTo(6));
        _eventRepo.Verify(r => r.Update(ev), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IncrementViewCountAsync_EventNotFound_DoesNothing()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.DoesNotThrowAsync(() => _sut.IncrementViewCountAsync(Guid.NewGuid()));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetAverageRatingAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetAverageRatingAsync_EventWithRatings_ReturnsAverage()
    {
        var ev = MakeEvent();
        ev.Ratings = new List<Rating>
        {
            new() { Id = Guid.NewGuid(), EventId = ev.Id, UserId = Guid.NewGuid(), Score = 4 },
            new() { Id = Guid.NewGuid(), EventId = ev.Id, UserId = Guid.NewGuid(), Score = 2 }
        };
        _eventRepo.Setup(r => r.GetWithDetailsAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.GetAverageRatingAsync(ev.Id);

        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public async Task GetAverageRatingAsync_EventWithNoRatings_ReturnsZero()
    {
        var ev = MakeEvent();
        ev.Ratings = new List<Rating>();
        _eventRepo.Setup(r => r.GetWithDetailsAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await _sut.GetAverageRatingAsync(ev.Id);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAverageRatingAsync_EventNotFound_ReturnsZero()
    {
        _eventRepo.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await _sut.GetAverageRatingAsync(Guid.NewGuid());

        Assert.That(result, Is.EqualTo(0));
    }

    // ── GetEventsInRangeAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetEventsInRangeAsync_ReturnsPublishedEventsInDateRange()
    {
        var start = DateTime.UtcNow;
        var end   = DateTime.UtcNow.AddDays(10);

        var inRange   = MakeEvent(EventStatus.Published);
        inRange.StartDate = DateTime.UtcNow.AddDays(2);
        inRange.EndDate   = DateTime.UtcNow.AddDays(5);

        var outOfRange = MakeEvent(EventStatus.Published);
        outOfRange.StartDate = DateTime.UtcNow.AddDays(15);
        outOfRange.EndDate   = DateTime.UtcNow.AddDays(16);

        _eventRepo.Setup(r => r.Query())
            .Returns(new List<Event> { inRange, outOfRange }.AsQueryable().BuildMock());

        var result = await _sut.GetEventsInRangeAsync(start, end);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(inRange.Id));
    }

    // ── CreateRecurringEventAsync (tests GenerateOccurrences) ────────────────

    [Test]
    public async Task CreateRecurringEventAsync_Daily_CreatesCorrectOccurrences()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);

        var series = new EventSeries { Id = Guid.NewGuid(), Title = "S", RecurrenceRule = "FREQ=DAILY;COUNT=3", OrganizerId = userId };
        _seriesRepo.Setup(r => r.AddAsync(It.IsAny<EventSeries>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var addedEvents = new List<Event>();
        _eventRepo.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => addedEvents.Add(e))
            .ReturnsAsync((Event e, CancellationToken _) => e);

        _slug.Setup(s => s.GenerateUniqueSlugAsync<Event>(
            It.IsAny<string>(), It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<string, Func<string, Task<bool>>, CancellationToken, ISlugService, string>(
                (text, _, _) => text.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N")[..6]);

        var template = new Event
        {
            Title       = "Daily Event",
            Description = "Desc",
            CategoryId  = 1,
            OrganizerId = userId,
            StartDate   = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDate     = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = await _sut.CreateRecurringEventAsync(template, "FREQ=DAILY;COUNT=3", maxOccurrences: 52);

        Assert.That(addedEvents, Has.Count.EqualTo(3));
        Assert.That(addedEvents.All(e => e.SeriesId == series.Id), Is.True);
        Assert.That(addedEvents.All(e => e.Status == EventStatus.Draft), Is.True);
    }

    [Test]
    public async Task CreateRecurringEventAsync_Weekly_CreatesCorrectOccurrences()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);

        var series = new EventSeries { Id = Guid.NewGuid(), Title = "S", RecurrenceRule = "FREQ=WEEKLY;COUNT=2;BYDAY=MO", OrganizerId = userId };
        _seriesRepo.Setup(r => r.AddAsync(It.IsAny<EventSeries>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var addedEvents = new List<Event>();
        _eventRepo.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => addedEvents.Add(e))
            .ReturnsAsync((Event e, CancellationToken _) => e);

        _slug.Setup(s => s.GenerateUniqueSlugAsync<Event>(
            It.IsAny<string>(), It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("slug-" + Guid.NewGuid().ToString("N")[..6]);

        var template = new Event
        {
            Title       = "Weekly Event",
            Description = "Desc",
            CategoryId  = 1,
            StartDate   = new DateTime(2025, 6, 2, 10, 0, 0, DateTimeKind.Utc), // Monday
            EndDate     = new DateTime(2025, 6, 2, 12, 0, 0, DateTimeKind.Utc)
        };

        await _sut.CreateRecurringEventAsync(template, "FREQ=WEEKLY;COUNT=2;BYDAY=MO", maxOccurrences: 52);

        Assert.That(addedEvents, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CreateRecurringEventAsync_Monthly_CreatesCorrectOccurrences()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);

        var series = new EventSeries { Id = Guid.NewGuid(), Title = "S", RecurrenceRule = "FREQ=MONTHLY;COUNT=3;BYMONTHDAY=15", OrganizerId = userId };
        _seriesRepo.Setup(r => r.AddAsync(It.IsAny<EventSeries>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var addedEvents = new List<Event>();
        _eventRepo.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => addedEvents.Add(e))
            .ReturnsAsync((Event e, CancellationToken _) => e);

        _slug.Setup(s => s.GenerateUniqueSlugAsync<Event>(
            It.IsAny<string>(), It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("slug");

        var template = new Event
        {
            Title       = "Monthly Event",
            Description = "Desc",
            CategoryId  = 1,
            StartDate   = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            EndDate     = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        };

        await _sut.CreateRecurringEventAsync(template, "FREQ=MONTHLY;COUNT=3;BYMONTHDAY=15", maxOccurrences: 52);

        Assert.That(addedEvents, Has.Count.EqualTo(3));
        Assert.That(addedEvents[0].StartDate.Day, Is.EqualTo(15));
        Assert.That(addedEvents[1].StartDate.Day, Is.EqualTo(15));
    }

    [Test]
    public async Task CreateRecurringEventAsync_NoCurrentUser_ThrowsForbiddenAccessException()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        Assert.ThrowsAsync<ForbiddenAccessException>(
            () => _sut.CreateRecurringEventAsync(MakeEvent(), "FREQ=DAILY;COUNT=1"));
    }

    // ── GetSeriesEventsAsync ─────────────────────────────────────────────────

    [Test]
    public async Task GetSeriesEventsAsync_ReturnsEventsForSeries()
    {
        var seriesId = Guid.NewGuid();
        var se1 = MakeEvent(); se1.SeriesId = seriesId;
        var se2 = MakeEvent(); se2.SeriesId = seriesId;
        var se3 = MakeEvent();
        var events = new List<Event> { se1, se2, se3 };
        events[0].StartDate = DateTime.UtcNow.AddDays(1);
        events[1].StartDate = DateTime.UtcNow.AddDays(2);

        _eventRepo.Setup(r => r.Query()).Returns(events.AsQueryable().BuildMock());

        var result = await _sut.GetSeriesEventsAsync(seriesId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(e => e.SeriesId == seriesId), Is.True);
    }
}
