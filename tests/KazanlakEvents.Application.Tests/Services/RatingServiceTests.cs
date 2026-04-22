using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class RatingServiceTests
{
    private Mock<IRepository<Rating>> _ratingRepo = null!;
    private Mock<IEventRepository>    _eventRepo = null!;
    private Mock<IUnitOfWork>         _uow = null!;
    private Mock<ICurrentUserService> _currentUser = null!;
    private Mock<ILogger<RatingService>> _logger = null!;
    private RatingService             _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ratingRepo  = new Mock<IRepository<Rating>>();
        _eventRepo   = new Mock<IEventRepository>();
        _uow         = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();
        _logger      = new Mock<ILogger<RatingService>>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new RatingService(_ratingRepo.Object, _eventRepo.Object, _uow.Object, _currentUser.Object, _logger.Object);
    }

    private static Event MakeCompletedEvent() => new()
    {
        Id          = Guid.NewGuid(),
        Title       = "Event",
        Slug        = "event",
        Description = "d",
        CategoryId  = 1,
        OrganizerId = Guid.NewGuid(),
        Status      = EventStatus.Completed,
        StartDate   = DateTime.UtcNow.AddDays(-2),
        EndDate     = DateTime.UtcNow.AddDays(-1)
    };

    // ── RateEventAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task RateEventAsync_ValidScore_CreatesRating()
    {
        var ev     = MakeCompletedEvent();
        var userId = Guid.NewGuid();
        Rating? captured = null;

        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _ratingRepo.Setup(r => r.AnyAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ratingRepo.Setup(r => r.AddAsync(It.IsAny<Rating>(), It.IsAny<CancellationToken>()))
            .Callback<Rating, CancellationToken>((rating, _) => captured = rating)
            .ReturnsAsync((Rating r, CancellationToken _) => r);

        var result = await _sut.RateEventAsync(ev.Id, userId, 4, "Great event!");

        Assert.That(captured,              Is.Not.Null);
        Assert.That(captured!.EventId,     Is.EqualTo(ev.Id));
        Assert.That(captured.UserId,       Is.EqualTo(userId));
        Assert.That(captured.Score,        Is.EqualTo(4));
        Assert.That(captured.ReviewText,   Is.EqualTo("Great event!"));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(0)]
    [TestCase(6)]
    [TestCase(-1)]
    public async Task RateEventAsync_InvalidScore_ThrowsArgumentOutOfRangeException(int score)
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.RateEventAsync(Guid.NewGuid(), Guid.NewGuid(), score));
    }

    [TestCase(1)]
    [TestCase(5)]
    public async Task RateEventAsync_BoundaryScores_AreAccepted(int score)
    {
        var ev     = MakeCompletedEvent();
        var userId = Guid.NewGuid();

        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _ratingRepo.Setup(r => r.AnyAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ratingRepo.Setup(r => r.AddAsync(It.IsAny<Rating>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rating r, CancellationToken _) => r);

        Assert.DoesNotThrowAsync(() => _sut.RateEventAsync(ev.Id, userId, score));
    }

    [Test]
    public async Task RateEventAsync_EventNotFound_ThrowsNotFoundException()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.RateEventAsync(Guid.NewGuid(), Guid.NewGuid(), 3));
    }

    [Test]
    public async Task RateEventAsync_EventNotCompleted_ThrowsInvalidOperationException()
    {
        var ev = new Event
        {
            Id          = Guid.NewGuid(),
            Title       = "Event",
            Slug        = "event",
            Description = "d",
            CategoryId  = 1,
            OrganizerId = Guid.NewGuid(),
            Status      = EventStatus.Published,
            StartDate   = DateTime.UtcNow.AddDays(1),
            EndDate     = DateTime.UtcNow.AddDays(2)
        };
        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RateEventAsync(ev.Id, Guid.NewGuid(), 3));
    }

    [Test]
    public async Task RateEventAsync_AlreadyRated_ThrowsInvalidOperationException()
    {
        var ev     = MakeCompletedEvent();
        var userId = Guid.NewGuid();

        _eventRepo.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _ratingRepo.Setup(r => r.AnyAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RateEventAsync(ev.Id, userId, 3));

        Assert.That(ex!.Message, Does.Contain("already rated"));
    }

    // ── GetUserRatingAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetUserRatingAsync_UserHasRated_ReturnsRating()
    {
        var eventId = Guid.NewGuid();
        var userId  = Guid.NewGuid();
        var rating  = new Rating { Id = Guid.NewGuid(), EventId = eventId, UserId = userId, Score = 5 };

        _ratingRepo.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rating> { rating }.AsReadOnly());

        var result = await _sut.GetUserRatingAsync(eventId, userId);

        Assert.That(result,       Is.Not.Null);
        Assert.That(result!.Score, Is.EqualTo(5));
    }

    [Test]
    public async Task GetUserRatingAsync_UserHasNotRated_ReturnsNull()
    {
        _ratingRepo.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rating>().AsReadOnly());

        var result = await _sut.GetUserRatingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ── GetEventRatingsAsync ─────────────────────────────────────────────────

    [Test]
    public async Task GetEventRatingsAsync_ReturnsAllRatingsForEvent()
    {
        var eventId = Guid.NewGuid();
        var ratings = new List<Rating>
        {
            new() { Id = Guid.NewGuid(), EventId = eventId, UserId = Guid.NewGuid(), Score = 3 },
            new() { Id = Guid.NewGuid(), EventId = eventId, UserId = Guid.NewGuid(), Score = 5 }
        };

        _ratingRepo.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratings.AsReadOnly());

        var result = await _sut.GetEventRatingsAsync(eventId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    // ── GetEventRatingSummaryAsync ───────────────────────────────────────────

    [Test]
    public async Task GetEventRatingSummaryAsync_WithRatings_ReturnsAverageAndCount()
    {
        var eventId = Guid.NewGuid();
        var ratings = new List<Rating>
        {
            new() { Id = Guid.NewGuid(), EventId = eventId, UserId = Guid.NewGuid(), Score = 4 },
            new() { Id = Guid.NewGuid(), EventId = eventId, UserId = Guid.NewGuid(), Score = 2 },
            new() { Id = Guid.NewGuid(), EventId = eventId, UserId = Guid.NewGuid(), Score = 3 }
        };

        _ratingRepo.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratings.AsReadOnly());

        var (average, count) = await _sut.GetEventRatingSummaryAsync(eventId);

        Assert.That(count,   Is.EqualTo(3));
        Assert.That(average, Is.EqualTo(3.0).Within(0.001));
    }

    [Test]
    public async Task GetEventRatingSummaryAsync_NoRatings_ReturnsZeroAndZero()
    {
        _ratingRepo.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Rating, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rating>().AsReadOnly());

        var (average, count) = await _sut.GetEventRatingSummaryAsync(Guid.NewGuid());

        Assert.That(count,   Is.EqualTo(0));
        Assert.That(average, Is.EqualTo(0.0));
    }
}
