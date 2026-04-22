using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserProfileRepository> _profileRepo = null!;
    private Mock<IApplicationDbContext>  _db = null!;
    private Mock<IEventRepository>       _eventRepo = null!;
    private Mock<IUnitOfWork>            _uow = null!;
    private Mock<ICurrentUserService>    _currentUser = null!;
    private Mock<INotificationService>   _notifications = null!;
    private Mock<ILogger<UserService>>   _logger = null!;
    private UserService                  _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _profileRepo   = new Mock<IUserProfileRepository>();
        _db            = new Mock<IApplicationDbContext>();
        _eventRepo     = new Mock<IEventRepository>();
        _uow           = new Mock<IUnitOfWork>();
        _currentUser   = new Mock<ICurrentUserService>();
        _notifications = new Mock<INotificationService>();
        _logger        = new Mock<ILogger<UserService>>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _notifications.Setup(n => n.SendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new UserService(
            _profileRepo.Object, _db.Object, _eventRepo.Object,
            _uow.Object, _currentUser.Object, _notifications.Object, _logger.Object);
    }

    private static UserProfile MakeProfile(Guid? userId = null) => new()
    {
        Id                = Guid.NewGuid(),
        UserId            = userId ?? Guid.NewGuid(),
        FirstName         = "John",
        LastName          = "Doe",
        PreferredLanguage = "en",
        CreatedAt         = DateTime.UtcNow
    };

    private static Event MakeEvent(Guid? organizerId = null) => new()
    {
        Id          = Guid.NewGuid(),
        Title       = "Event",
        Slug        = "event-" + Guid.NewGuid().ToString("N")[..6],
        Description = "d",
        CategoryId  = 1,
        OrganizerId = organizerId ?? Guid.NewGuid(),
        Status      = EventStatus.Published,
        StartDate   = DateTime.UtcNow.AddDays(1),
        EndDate     = DateTime.UtcNow.AddDays(2)
    };

    // ── GetProfileAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
    {
        var userId  = Guid.NewGuid();
        var profile = MakeProfile(userId);
        _profileRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _sut.GetProfileAsync(userId);

        Assert.That(result,         Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task GetProfileAsync_NonExistentUser_ReturnsNull()
    {
        _profileRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var result = await _sut.GetProfileAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ── UpdateProfileAsync ───────────────────────────────────────────────────

    [Test]
    public async Task UpdateProfileAsync_ExistingProfile_UpdatesAndSaves()
    {
        var userId   = Guid.NewGuid();
        var existing = MakeProfile(userId);
        _profileRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var updated = new UserProfile
        {
            UserId            = userId,
            FirstName         = "Jane",
            LastName          = "Smith",
            Bio               = "Developer",
            AvatarUrl         = "https://avatar.url",
            DateOfBirth       = new DateTime(1990, 1, 1),
            City              = "Plovdiv",
            PhoneNumber       = "+359888000000",
            PreferredLanguage = "bg"
        };

        var result = await _sut.UpdateProfileAsync(updated);

        Assert.That(result.FirstName,         Is.EqualTo("Jane"));
        Assert.That(result.LastName,          Is.EqualTo("Smith"));
        Assert.That(result.Bio,               Is.EqualTo("Developer"));
        Assert.That(result.City,              Is.EqualTo("Plovdiv"));
        Assert.That(result.PreferredLanguage, Is.EqualTo("bg"));
        _profileRepo.Verify(r => r.Update(existing), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateProfileAsync_ProfileNotFound_ThrowsNotFoundException()
    {
        _profileRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateProfileAsync(new UserProfile { UserId = Guid.NewGuid() }));
    }

    // ── FollowAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task FollowAsync_ValidFollow_AddsFollowAndSendsNotification()
    {
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();

        var follows = new List<Follow>();
        var mockSet = follows.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<Follow>())).Callback<Follow>(follows.Add);
        _db.Setup(d => d.Follows).Returns(mockSet.Object);

        await _sut.FollowAsync(followerId, followeeId);

        Assert.That(follows,              Has.Count.EqualTo(1));
        Assert.That(follows[0].FollowerId, Is.EqualTo(followerId));
        Assert.That(follows[0].FolloweeId, Is.EqualTo(followeeId));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.SendNotificationAsync(
            followeeId, NotificationType.NewFollower,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task FollowAsync_SameUser_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.FollowAsync(userId, userId));

        Assert.That(ex!.Message, Does.Contain("cannot follow themselves"));
    }

    [Test]
    public async Task FollowAsync_AlreadyFollowing_ThrowsInvalidOperationException()
    {
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var existing   = new Follow { FollowerId = followerId, FolloweeId = followeeId };

        _db.Setup(d => d.Follows)
           .Returns(new List<Follow> { existing }.AsQueryable().BuildMockDbSet().Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.FollowAsync(followerId, followeeId));

        Assert.That(ex!.Message, Does.Contain("Already following"));
    }

    // ── UnfollowAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task UnfollowAsync_ExistingFollow_RemovesAndSaves()
    {
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var follow     = new Follow { FollowerId = followerId, FolloweeId = followeeId };
        var follows    = new List<Follow> { follow };
        var mockSet    = follows.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Remove(It.IsAny<Follow>())).Callback<Follow>(f => follows.Remove(f));
        _db.Setup(d => d.Follows).Returns(mockSet.Object);

        await _sut.UnfollowAsync(followerId, followeeId);

        Assert.That(follows, Is.Empty);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UnfollowAsync_NotFollowing_ThrowsNotFoundException()
    {
        _db.Setup(d => d.Follows)
           .Returns(new List<Follow>().AsQueryable().BuildMockDbSet().Object);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UnfollowAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── IsFollowingAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task IsFollowingAsync_WhenFollowing_ReturnsTrue()
    {
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        var follows = new List<Follow> { new() { FollowerId = followerId, FolloweeId = followeeId } };
        _db.Setup(d => d.Follows).Returns(follows.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.IsFollowingAsync(followerId, followeeId);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsFollowingAsync_WhenNotFollowing_ReturnsFalse()
    {
        _db.Setup(d => d.Follows)
           .Returns(new List<Follow>().AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.IsFollowingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    // ── GetFollowerCountAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetFollowerCountAsync_ReturnsCountOfFollowers()
    {
        var userId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            new() { FollowerId = Guid.NewGuid(), FolloweeId = userId },
            new() { FollowerId = Guid.NewGuid(), FolloweeId = userId },
            new() { FollowerId = userId,          FolloweeId = Guid.NewGuid() }  // outgoing — not counted
        };
        _db.Setup(d => d.Follows).Returns(follows.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetFollowerCountAsync(userId);

        Assert.That(result, Is.EqualTo(2));
    }

    // ── GetFollowingCountAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetFollowingCountAsync_ReturnsCountOfFollowing()
    {
        var userId = Guid.NewGuid();
        var follows = new List<Follow>
        {
            new() { FollowerId = userId,          FolloweeId = Guid.NewGuid() },
            new() { FollowerId = userId,          FolloweeId = Guid.NewGuid() },
            new() { FollowerId = Guid.NewGuid(), FolloweeId = userId }  // incoming — not counted
        };
        _db.Setup(d => d.Follows).Returns(follows.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetFollowingCountAsync(userId);

        Assert.That(result, Is.EqualTo(2));
    }

    // ── GetOrganizedEventCountAsync ──────────────────────────────────────────

    [Test]
    public async Task GetOrganizedEventCountAsync_ReturnsCountForOrganizer()
    {
        var userId = Guid.NewGuid();
        var events = new List<Event>
        {
            MakeEvent(userId),
            MakeEvent(userId),
            MakeEvent()
        };
        _db.Setup(d => d.Events).Returns(events.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetOrganizedEventCountAsync(userId);

        Assert.That(result, Is.EqualTo(2));
    }

    // ── GetAttendedEventCountAsync ───────────────────────────────────────────

    [Test]
    public async Task GetAttendedEventCountAsync_ReturnsCorrectCount()
    {
        var userId = Guid.NewGuid();
        var attendances = new List<EventAttendance>
        {
            new() { UserId = userId,          EventId = Guid.NewGuid(), Status = AttendanceStatus.Going },
            new() { UserId = userId,          EventId = Guid.NewGuid(), Status = AttendanceStatus.Going },
            new() { UserId = Guid.NewGuid(), EventId = Guid.NewGuid(), Status = AttendanceStatus.Going }
        };
        _db.Setup(d => d.EventAttendances).Returns(attendances.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAttendedEventCountAsync(userId);

        Assert.That(result, Is.EqualTo(2));
    }

    // ── FavoriteEventAsync ───────────────────────────────────────────────────

    [Test]
    public async Task FavoriteEventAsync_ValidEvent_AddsFavoriteAndSaves()
    {
        var userId  = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ev      = MakeEvent();
        ev.Id = eventId;

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var favorites = new List<Favorite>();
        var mockSet   = favorites.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<Favorite>())).Callback<Favorite>(favorites.Add);
        _db.Setup(d => d.Favorites).Returns(mockSet.Object);

        await _sut.FavoriteEventAsync(userId, eventId);

        Assert.That(favorites,            Has.Count.EqualTo(1));
        Assert.That(favorites[0].UserId,  Is.EqualTo(userId));
        Assert.That(favorites[0].EventId, Is.EqualTo(eventId));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task FavoriteEventAsync_EventNotFound_ThrowsNotFoundException()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.FavoriteEventAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public async Task FavoriteEventAsync_AlreadyFavorited_ThrowsInvalidOperationException()
    {
        var userId  = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ev      = MakeEvent();
        ev.Id = eventId;

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var existing  = new Favorite { UserId = userId, EventId = eventId, Event = ev };
        _db.Setup(d => d.Favorites)
           .Returns(new List<Favorite> { existing }.AsQueryable().BuildMockDbSet().Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.FavoriteEventAsync(userId, eventId));

        Assert.That(ex!.Message, Does.Contain("already in favorites"));
    }

    // ── UnfavoriteEventAsync ─────────────────────────────────────────────────

    [Test]
    public async Task UnfavoriteEventAsync_ExistingFavorite_RemovesAndSaves()
    {
        var userId    = Guid.NewGuid();
        var eventId   = Guid.NewGuid();
        var ev        = MakeEvent();
        ev.Id         = eventId;
        var favorite  = new Favorite { UserId = userId, EventId = eventId, Event = ev };
        var favorites = new List<Favorite> { favorite };
        var mockSet   = favorites.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Remove(It.IsAny<Favorite>())).Callback<Favorite>(f => favorites.Remove(f));
        _db.Setup(d => d.Favorites).Returns(mockSet.Object);

        await _sut.UnfavoriteEventAsync(userId, eventId);

        Assert.That(favorites, Is.Empty);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UnfavoriteEventAsync_NotFavorited_ThrowsNotFoundException()
    {
        _db.Setup(d => d.Favorites)
           .Returns(new List<Favorite>().AsQueryable().BuildMockDbSet().Object);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UnfavoriteEventAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── HasFavoritedAsync ────────────────────────────────────────────────────

    [Test]
    public async Task HasFavoritedAsync_WhenFavorited_ReturnsTrue()
    {
        var userId  = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ev      = MakeEvent();
        ev.Id       = eventId;
        var favorites = new List<Favorite> { new() { UserId = userId, EventId = eventId, Event = ev } };
        _db.Setup(d => d.Favorites).Returns(favorites.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.HasFavoritedAsync(userId, eventId);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HasFavoritedAsync_WhenNotFavorited_ReturnsFalse()
    {
        _db.Setup(d => d.Favorites)
           .Returns(new List<Favorite>().AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.HasFavoritedAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    // ── GetFavoriteEventsAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetFavoriteEventsAsync_ReturnsEventsForUser()
    {
        var userId  = Guid.NewGuid();
        var ev1     = MakeEvent();
        var ev2     = MakeEvent();
        var otherId = Guid.NewGuid();

        var favorites = new List<Favorite>
        {
            new() { UserId = userId,  EventId = ev1.Id, Event = ev1 },
            new() { UserId = userId,  EventId = ev2.Id, Event = ev2 },
            new() { UserId = otherId, EventId = ev1.Id, Event = ev1 }
        };
        _db.Setup(d => d.Favorites).Returns(favorites.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetFavoriteEventsAsync(userId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetFavoriteEventsAsync_NoFavorites_ReturnsEmptyList()
    {
        _db.Setup(d => d.Favorites)
           .Returns(new List<Favorite>().AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetFavoriteEventsAsync(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }
}
