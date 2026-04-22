using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Domain.Interfaces;

public interface IUserProfileRepository : IRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfile?> GetWithUserAsync(Guid userId, CancellationToken ct = default);
}
