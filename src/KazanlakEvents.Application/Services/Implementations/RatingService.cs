using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Services.Implementations;

public class RatingService(
    IRepository<Rating> ratingRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<RatingService> logger) : IRatingService
{
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<Rating> RateEventAsync(
        Guid eventId, Guid userId, int score, string? reviewText = null,
        CancellationToken ct = default)
    {
        if (score is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 1 and 5.");

        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(KazanlakEvents.Domain.Entities.Event), eventId);

        if (ev.Status != EventStatus.Completed)
            throw new InvalidOperationException(
                $"Only completed events can be rated. Current status: {ev.Status}.");

        if (await ratingRepository.AnyAsync(r => r.EventId == eventId && r.UserId == userId, ct))
            throw new InvalidOperationException("User has already rated this event.");

        var rating = new Rating
        {
            EventId    = eventId,
            UserId     = userId,
            Score      = score,
            ReviewText = reviewText,
            CreatedAt  = DateTime.UtcNow
        };

        await ratingRepository.AddAsync(rating, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "User {UserId} rated Event {EventId} with score {Score}", userId, eventId, score);

        return rating;
    }

    public async Task<Rating?> GetUserRatingAsync(
        Guid eventId, Guid userId, CancellationToken ct = default)
    {
        var results = await ratingRepository.FindAsync(
            r => r.EventId == eventId && r.UserId == userId, ct);
        return results.FirstOrDefault();
    }

    public async Task<IReadOnlyList<Rating>> GetEventRatingsAsync(
        Guid eventId, CancellationToken ct = default)
    {
        return await ratingRepository.FindAsync(r => r.EventId == eventId, ct);
    }

    public async Task<(double Average, int Count)> GetEventRatingSummaryAsync(
        Guid eventId, CancellationToken ct = default)
    {
        var ratings = await ratingRepository.FindAsync(r => r.EventId == eventId, ct);
        if (ratings.Count == 0)
            return (0.0, 0);

        return (ratings.Average(r => r.Score), ratings.Count);
    }
}
