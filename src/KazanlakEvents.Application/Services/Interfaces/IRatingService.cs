using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface IRatingService
{
    Task<Rating> RateEventAsync(Guid eventId, Guid userId, int score, string? reviewText = null, CancellationToken ct = default);
    Task<Rating?> GetUserRatingAsync(Guid eventId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Rating>> GetEventRatingsAsync(Guid eventId, CancellationToken ct = default);
    Task<(double Average, int Count)> GetEventRatingSummaryAsync(Guid eventId, CancellationToken ct = default);
}
