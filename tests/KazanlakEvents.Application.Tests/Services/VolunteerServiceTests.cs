using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class VolunteerServiceTests
{
    private Mock<IRepository<VolunteerTask>>   _taskRepo = null!;
    private Mock<IRepository<VolunteerShift>>  _shiftRepo = null!;
    private Mock<IRepository<VolunteerSignup>> _signupRepo = null!;
    private Mock<IUnitOfWork>                  _uow = null!;
    private Mock<ICurrentUserService>          _currentUser = null!;
    private Mock<INotificationService>         _notifications = null!;
    private VolunteerService                   _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _taskRepo      = new Mock<IRepository<VolunteerTask>>();
        _shiftRepo     = new Mock<IRepository<VolunteerShift>>();
        _signupRepo    = new Mock<IRepository<VolunteerSignup>>();
        _uow           = new Mock<IUnitOfWork>();
        _currentUser   = new Mock<ICurrentUserService>();
        _notifications = new Mock<INotificationService>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _notifications.Setup(n => n.SendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new VolunteerService(
            _taskRepo.Object, _shiftRepo.Object, _signupRepo.Object,
            _uow.Object, _currentUser.Object, _notifications.Object);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Event MakeEvent(Guid? organizerId = null) => new()
    {
        Id          = Guid.NewGuid(),
        Title       = "Event",
        Slug        = "event-slug",
        Description = "d",
        CategoryId  = 1,
        OrganizerId = organizerId ?? Guid.NewGuid(),
        Status      = EventStatus.Published,
        StartDate   = DateTime.UtcNow.AddDays(1),
        EndDate     = DateTime.UtcNow.AddDays(2)
    };

    private static VolunteerTask MakeTask(Guid? eventId = null, Event? ev = null)
    {
        var eId = eventId ?? ev?.Id ?? Guid.NewGuid();
        return new VolunteerTask
        {
            Id               = Guid.NewGuid(),
            EventId          = eId,
            Name             = "Setup",
            VolunteersNeeded = 5,
            Event            = ev ?? MakeEvent()
        };
    }

    private static VolunteerShift MakeShift(
        VolunteerTask? task = null,
        int maxVolunteers = 5,
        List<VolunteerSignup>? signups = null)
    {
        var t = task ?? MakeTask();
        return new VolunteerShift
        {
            Id            = Guid.NewGuid(),
            TaskId        = t.Id,
            Task          = t,
            StartTime     = DateTime.UtcNow.AddDays(1),
            EndTime       = DateTime.UtcNow.AddDays(1).AddHours(4),
            MaxVolunteers = maxVolunteers,
            Signups       = signups ?? new List<VolunteerSignup>()
        };
    }

    // ── CreateTaskAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task CreateTaskAsync_AddsAndSavesTask()
    {
        var task = MakeTask();
        _taskRepo.Setup(r => r.AddAsync(task, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var result = await _sut.CreateTaskAsync(task);

        Assert.That(result, Is.EqualTo(task));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── CreateShiftAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task CreateShiftAsync_ExistingTask_AddsAndSavesShift()
    {
        var task  = MakeTask();
        var shift = MakeShift(task);

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _shiftRepo.Setup(r => r.AddAsync(shift, It.IsAny<CancellationToken>())).ReturnsAsync(shift);

        var result = await _sut.CreateShiftAsync(shift);

        Assert.That(result, Is.EqualTo(shift));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateShiftAsync_TaskNotFound_ThrowsNotFoundException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VolunteerTask?)null);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateShiftAsync(MakeShift()));
    }

    // ── SignUpAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task SignUpAsync_ValidShiftAndUser_CreatesSignup()
    {
        var ev    = MakeEvent();
        var task  = MakeTask(ev: ev);
        var shift = MakeShift(task: task, maxVolunteers: 5);
        var userId = Guid.NewGuid();

        var shifts = new List<VolunteerShift> { shift };
        _shiftRepo.Setup(r => r.Query()).Returns(shifts.AsQueryable().BuildMock());

        VolunteerSignup? captured = null;
        _signupRepo.Setup(r => r.AddAsync(It.IsAny<VolunteerSignup>(), It.IsAny<CancellationToken>()))
            .Callback<VolunteerSignup, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((VolunteerSignup s, CancellationToken _) => s);

        var result = await _sut.SignUpAsync(shift.Id, userId);

        Assert.That(captured,              Is.Not.Null);
        Assert.That(captured!.ShiftId,     Is.EqualTo(shift.Id));
        Assert.That(captured.UserId,       Is.EqualTo(userId));
        Assert.That(captured.Status,       Is.EqualTo(VolunteerSignupStatus.Registered));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.SendNotificationAsync(
            ev.OrganizerId, NotificationType.VolunteerSignup,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SignUpAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        _shiftRepo.Setup(r => r.Query())
            .Returns(new List<VolunteerShift>().AsQueryable().BuildMock());

        Assert.ThrowsAsync<NotFoundException>(() => _sut.SignUpAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public async Task SignUpAsync_AlreadySignedUp_ThrowsInvalidOperationException()
    {
        var userId   = Guid.NewGuid();
        var ev       = MakeEvent();
        var task     = MakeTask(ev: ev);
        var existing = new VolunteerSignup
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Status    = VolunteerSignupStatus.Registered,
            SignedUpAt = DateTime.UtcNow
        };
        var shift = MakeShift(task: task, maxVolunteers: 5, signups: new List<VolunteerSignup> { existing });
        existing.ShiftId = shift.Id;

        _shiftRepo.Setup(r => r.Query())
            .Returns(new List<VolunteerShift> { shift }.AsQueryable().BuildMock());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SignUpAsync(shift.Id, userId));

        Assert.That(ex!.Message, Does.Contain("already signed up"));
    }

    [Test]
    public async Task SignUpAsync_ShiftFull_ThrowsInvalidOperationException()
    {
        var ev   = MakeEvent();
        var task = MakeTask(ev: ev);

        // 2 signups, max 2 — shift is full
        var signups = new[]
        {
            new VolunteerSignup { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = VolunteerSignupStatus.Registered, SignedUpAt = DateTime.UtcNow },
            new VolunteerSignup { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = VolunteerSignupStatus.Confirmed,  SignedUpAt = DateTime.UtcNow }
        };
        var shift = MakeShift(task: task, maxVolunteers: 2, signups: signups.ToList());

        _shiftRepo.Setup(r => r.Query())
            .Returns(new List<VolunteerShift> { shift }.AsQueryable().BuildMock());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SignUpAsync(shift.Id, Guid.NewGuid()));

        Assert.That(ex!.Message, Does.Contain("already full"));
    }

    // ── UpdateSignupStatusAsync ──────────────────────────────────────────────

    [Test]
    public async Task UpdateSignupStatusAsync_ExistingSignup_UpdatesStatus()
    {
        var signupId = Guid.NewGuid();
        var signup = new VolunteerSignup
        {
            Id        = signupId,
            ShiftId   = Guid.NewGuid(),
            UserId    = Guid.NewGuid(),
            Status    = VolunteerSignupStatus.Registered,
            SignedUpAt = DateTime.UtcNow
        };
        _signupRepo.Setup(r => r.GetByIdAsync(signupId, It.IsAny<CancellationToken>())).ReturnsAsync(signup);

        var result = await _sut.UpdateSignupStatusAsync(signupId, VolunteerSignupStatus.Confirmed);

        Assert.That(result.Status, Is.EqualTo(VolunteerSignupStatus.Confirmed));
        _signupRepo.Verify(r => r.Update(signup), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateSignupStatusAsync_SignupNotFound_ThrowsNotFoundException()
    {
        _signupRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VolunteerSignup?)null);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateSignupStatusAsync(Guid.NewGuid(), VolunteerSignupStatus.Completed));
    }

    // ── LogHoursAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task LogHoursAsync_ExistingSignup_SetsHoursLogged()
    {
        var signupId = Guid.NewGuid();
        var signup = new VolunteerSignup
        {
            Id         = signupId,
            ShiftId    = Guid.NewGuid(),
            UserId     = Guid.NewGuid(),
            Status     = VolunteerSignupStatus.Completed,
            SignedUpAt = DateTime.UtcNow
        };
        _signupRepo.Setup(r => r.GetByIdAsync(signupId, It.IsAny<CancellationToken>())).ReturnsAsync(signup);

        await _sut.LogHoursAsync(signupId, 4.5m);

        Assert.That(signup.HoursLogged, Is.EqualTo(4.5m));
        _signupRepo.Verify(r => r.Update(signup), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LogHoursAsync_SignupNotFound_ThrowsNotFoundException()
    {
        _signupRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VolunteerSignup?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.LogHoursAsync(Guid.NewGuid(), 3m));
    }

    // ── GetEventTasksAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetEventTasksAsync_ReturnsTasksForEvent_OrderedByName()
    {
        var eventId = Guid.NewGuid();
        var tasks = new List<VolunteerTask>
        {
            new() { Id = Guid.NewGuid(), EventId = eventId, Name = "Zebra task",  VolunteersNeeded = 2, Shifts = new List<VolunteerShift>() },
            new() { Id = Guid.NewGuid(), EventId = eventId, Name = "Alpha task",  VolunteersNeeded = 1, Shifts = new List<VolunteerShift>() },
            new() { Id = Guid.NewGuid(), EventId = Guid.NewGuid(), Name = "Other task", VolunteersNeeded = 1, Shifts = new List<VolunteerShift>() }
        };
        _taskRepo.Setup(r => r.Query()).Returns(tasks.AsQueryable().BuildMock());

        var result = await _sut.GetEventTasksAsync(eventId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Alpha task"));
        Assert.That(result[1].Name, Is.EqualTo("Zebra task"));
    }

    // ── GetUserSignupsAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetUserSignupsAsync_ReturnsSignupsForUser_OrderedDescBySignedUpAt()
    {
        var userId  = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var ev      = MakeEvent();
        var task    = MakeTask(ev: ev);
        var shift   = MakeShift(task: task);
        var now     = DateTime.UtcNow;

        var signups = new List<VolunteerSignup>
        {
            new() { Id = Guid.NewGuid(), UserId = userId,  ShiftId = shift.Id, Shift = shift, Status = VolunteerSignupStatus.Registered, SignedUpAt = now.AddDays(-2) },
            new() { Id = Guid.NewGuid(), UserId = userId,  ShiftId = shift.Id, Shift = shift, Status = VolunteerSignupStatus.Confirmed,  SignedUpAt = now.AddDays(-1) },
            new() { Id = Guid.NewGuid(), UserId = otherId, ShiftId = shift.Id, Shift = shift, Status = VolunteerSignupStatus.Registered, SignedUpAt = now }
        };
        _signupRepo.Setup(r => r.Query()).Returns(signups.AsQueryable().BuildMock());

        var result = await _sut.GetUserSignupsAsync(userId);

        Assert.That(result, Has.Count.EqualTo(2));
        // Most recent first
        Assert.That(result[0].SignedUpAt, Is.GreaterThan(result[1].SignedUpAt));
    }

    // ── DeleteTaskAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task DeleteTaskAsync_ExistingTask_RemovesAndSaves()
    {
        var task = MakeTask();
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.DeleteTaskAsync(task.Id);

        _taskRepo.Verify(r => r.Remove(task), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteTaskAsync_TaskNotFound_ThrowsNotFoundException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VolunteerTask?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteTaskAsync(Guid.NewGuid()));
    }

    // ── DeleteShiftAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task DeleteShiftAsync_ExistingShift_RemovesAndSaves()
    {
        var shift = MakeShift();
        _shiftRepo.Setup(r => r.GetByIdAsync(shift.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shift);

        await _sut.DeleteShiftAsync(shift.Id);

        _shiftRepo.Verify(r => r.Remove(shift), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteShiftAsync_ShiftNotFound_ThrowsNotFoundException()
    {
        _shiftRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VolunteerShift?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteShiftAsync(Guid.NewGuid()));
    }

    // ── RemoveSignupAsync ────────────────────────────────────────────────────

    [Test]
    public async Task RemoveSignupAsync_ExistingSignup_RemovesAndReturnsInfo()
    {
        var ev        = MakeEvent();
        var task      = MakeTask(ev: ev);
        var shift     = MakeShift(task: task);
        var userId    = Guid.NewGuid();
        var signupId  = Guid.NewGuid();

        var signup = new VolunteerSignup
        {
            Id         = signupId,
            UserId     = userId,
            ShiftId    = shift.Id,
            Shift      = shift,
            Status     = VolunteerSignupStatus.Registered,
            SignedUpAt = DateTime.UtcNow
        };

        _signupRepo.Setup(r => r.Query())
            .Returns(new List<VolunteerSignup> { signup }.AsQueryable().BuildMock());

        var (volunteerUserId, taskName, eventId) = await _sut.RemoveSignupAsync(signupId);

        Assert.That(volunteerUserId, Is.EqualTo(userId));
        Assert.That(taskName,        Is.EqualTo(task.Name));
        Assert.That(eventId,         Is.EqualTo(ev.Id));
        _signupRepo.Verify(r => r.Remove(signup), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveSignupAsync_SignupNotFound_ThrowsNotFoundException()
    {
        _signupRepo.Setup(r => r.Query())
            .Returns(new List<VolunteerSignup>().AsQueryable().BuildMock());

        Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveSignupAsync(Guid.NewGuid()));
    }

    // ── GetVolunteerStatsAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetVolunteerStatsAsync_CompletedSignups_ReturnsTotalHoursAndEventCount()
    {
        var userId  = Guid.NewGuid();
        var ev1     = MakeEvent();
        var ev2     = MakeEvent();
        var task1   = MakeTask(ev: ev1);
        var task2   = MakeTask(ev: ev2);
        var shift1  = MakeShift(task: task1);
        var shift2  = MakeShift(task: task2);

        var signups = new List<VolunteerSignup>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ShiftId = shift1.Id, Shift = shift1, Status = VolunteerSignupStatus.Completed,  HoursLogged = 3.0m, SignedUpAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = userId, ShiftId = shift2.Id, Shift = shift2, Status = VolunteerSignupStatus.Completed,  HoursLogged = 4.5m, SignedUpAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = userId, ShiftId = shift1.Id, Shift = shift1, Status = VolunteerSignupStatus.Registered, HoursLogged = 8.0m, SignedUpAt = DateTime.UtcNow }  // not completed
        };
        _signupRepo.Setup(r => r.Query()).Returns(signups.AsQueryable().BuildMock());

        var (totalHours, eventCount) = await _sut.GetVolunteerStatsAsync(userId);

        Assert.That(totalHours,  Is.EqualTo(7));   // 3 + 4 (truncated to int)
        Assert.That(eventCount,  Is.EqualTo(2));    // ev1 and ev2
    }

    [Test]
    public async Task GetVolunteerStatsAsync_NoCompletedSignups_ReturnsZeros()
    {
        var userId = Guid.NewGuid();
        _signupRepo.Setup(r => r.Query()).Returns(new List<VolunteerSignup>().AsQueryable().BuildMock());

        var (totalHours, eventCount) = await _sut.GetVolunteerStatsAsync(userId);

        Assert.That(totalHours, Is.EqualTo(0));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetVolunteerStatsAsync_NullHoursLogged_TreatedAsZero()
    {
        var userId  = Guid.NewGuid();
        var ev      = MakeEvent();
        var task    = MakeTask(ev: ev);
        var shift   = MakeShift(task: task);

        var signups = new List<VolunteerSignup>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ShiftId = shift.Id, Shift = shift, Status = VolunteerSignupStatus.Completed, HoursLogged = null, SignedUpAt = DateTime.UtcNow }
        };
        _signupRepo.Setup(r => r.Query()).Returns(signups.AsQueryable().BuildMock());

        var (totalHours, eventCount) = await _sut.GetVolunteerStatsAsync(userId);

        Assert.That(totalHours, Is.EqualTo(0));
        Assert.That(eventCount, Is.EqualTo(1));
    }
}
