using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Data;

namespace KazanlakEvents.Infrastructure.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public void Dispose() => context.Dispose();
}
