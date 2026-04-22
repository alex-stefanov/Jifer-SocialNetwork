using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface IUserService
{
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfile> UpdateProfileAsync(UserProfile profile, CancellationToken ct = default);

    Task FollowAsync(Guid followerId, Guid followeeId, CancellationToken ct = default);
    Task UnfollowAsync(Guid followerId, Guid followeeId, CancellationToken ct = default);
    Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId, CancellationToken ct = default);
    Task<int> GetFollowerCountAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetFollowingCountAsync(Guid userId, CancellationToken ct = default);

    Task<int> GetOrganizedEventCountAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetAttendedEventCountAsync(Guid userId, CancellationToken ct = default);

    Task FavoriteEventAsync(Guid userId, Guid eventId, CancellationToken ct = default);
    Task UnfavoriteEventAsync(Guid userId, Guid eventId, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetFavoriteEventsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetFavoriteEventsWithDetailsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasFavoritedAsync(Guid userId, Guid eventId, CancellationToken ct = default);
}
