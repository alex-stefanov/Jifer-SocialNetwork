using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Services.Implementations;

public class UserService(
    IUserProfileRepository userProfileRepository,
    IApplicationDbContext db,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ILogger<UserService> logger) : IUserService
{
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken ct = default)
        => await userProfileRepository.GetByUserIdAsync(userId, ct);

    public async Task<UserProfile> UpdateProfileAsync(UserProfile profile, CancellationToken ct = default)
    {
        var existing = await userProfileRepository.GetByUserIdAsync(profile.UserId, ct)
            ?? throw new NotFoundException(nameof(UserProfile), profile.UserId);

        existing.FirstName         = profile.FirstName;
        existing.LastName          = profile.LastName;
        existing.Bio               = profile.Bio;
        existing.AvatarUrl         = profile.AvatarUrl;
        existing.DateOfBirth       = profile.DateOfBirth;
        existing.City              = profile.City;
        existing.PhoneNumber       = profile.PhoneNumber;
        existing.PreferredLanguage = profile.PreferredLanguage;

        userProfileRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);
        return existing;
    }

    public async Task FollowAsync(Guid followerId, Guid followeeId, CancellationToken ct = default)
    {
        if (followerId == followeeId)
            throw new InvalidOperationException("A user cannot follow themselves.");

        if (await db.Follows.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct))
            throw new InvalidOperationException("Already following this user.");

        db.Follows.Add(new Follow
        {
            FollowerId  = followerId,
            FolloweeId  = followeeId,
            FollowedAt  = DateTime.UtcNow
        });
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("User {Follower} followed User {Followee}", followerId, followeeId);

        await notificationService.SendNotificationAsync(
            followeeId,
            NotificationType.NewFollower,
            "New follower",
            "Someone started following you.",
            linkUrl: $"/Profile/Index?id={followerId}",
            ct: ct);
    }

    public async Task UnfollowAsync(Guid followerId, Guid followeeId, CancellationToken ct = default)
    {
        var follow = await db.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct)
            ?? throw new NotFoundException("Follow", $"{followerId}->{followeeId}");

        db.Follows.Remove(follow);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("User {Follower} unfollowed User {Followee}", followerId, followeeId);
    }

    public async Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId, CancellationToken ct = default)
        => await db.Follows.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct);

    public async Task<int> GetFollowerCountAsync(Guid userId, CancellationToken ct = default)
        => await db.Follows.CountAsync(f => f.FolloweeId == userId, ct);

    public async Task<int> GetFollowingCountAsync(Guid userId, CancellationToken ct = default)
        => await db.Follows.CountAsync(f => f.FollowerId == userId, ct);

    public async Task<int> GetOrganizedEventCountAsync(Guid userId, CancellationToken ct = default)
        => await db.Events.CountAsync(e => e.OrganizerId == userId, ct);

    public async Task<int> GetAttendedEventCountAsync(Guid userId, CancellationToken ct = default)
        => await db.EventAttendances.CountAsync(ea => ea.UserId == userId, ct);

    public async Task FavoriteEventAsync(Guid userId, Guid eventId, CancellationToken ct = default)
    {
        _ = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException(nameof(KazanlakEvents.Domain.Entities.Event), eventId);

        if (await db.Favorites.AnyAsync(f => f.UserId == userId && f.EventId == eventId, ct))
            throw new InvalidOperationException("Event is already in favorites.");

        db.Favorites.Add(new Favorite
        {
            UserId    = userId,
            EventId   = eventId,
            CreatedAt = DateTime.UtcNow
        });
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} favorited Event {EventId}", userId, eventId);
    }

    public async Task UnfavoriteEventAsync(Guid userId, Guid eventId, CancellationToken ct = default)
    {
        var fav = await db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.EventId == eventId, ct)
            ?? throw new NotFoundException("Favorite", eventId);

        db.Favorites.Remove(fav);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Entities.Event>> GetFavoriteEventsAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await db.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Event).ThenInclude(e => e.Category)
            .Include(f => f.Event).ThenInclude(e => e.Venue)
            .Select(f => f.Event)
            .Where(e => e != null)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Entities.Event>> GetFavoriteEventsWithDetailsAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await db.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Event).ThenInclude(e => e.Category)
            .Include(f => f.Event).ThenInclude(e => e.Venue)
            .Include(f => f.Event).ThenInclude(e => e.Attendances)
            .Include(f => f.Event).ThenInclude(e => e.Ratings)
            .Include(f => f.Event).ThenInclude(e => e.Comments)
            .Include(f => f.Event).ThenInclude(e => e.TicketTypes)
            .Select(f => f.Event)
            .Where(e => e != null)
            .ToListAsync(ct);
    }

    public async Task<bool> HasFavoritedAsync(Guid userId, Guid eventId, CancellationToken ct = default)
        => await db.Favorites.AnyAsync(f => f.UserId == userId && f.EventId == eventId, ct);
}
