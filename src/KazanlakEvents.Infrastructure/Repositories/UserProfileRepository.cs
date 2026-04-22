using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Infrastructure.Repositories;

public class UserProfileRepository : Repository<UserProfile>, IUserProfileRepository
{
    private readonly ApplicationDbContext _context;

    public UserProfileRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    // Identity user data is accessed via UserManager<ApplicationUser> in the service layer.
    // This method returns the profile record; callers combine it with Identity if needed.
    public async Task<UserProfile?> GetWithUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
}
