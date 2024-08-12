namespace Jifer.Data.Repositories
{
    using Jifer.Data.Models;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Repository : IRepository
    {
        private readonly JiferDbContext _context;

        public Repository(JiferDbContext dbContext)
        {
            _context = dbContext;
        }

        protected DbSet<T> DbSet<T>() where T : class
        {
            return _context.Set<T>();
        }

        public async Task AddAsync<T>(T entity) where T : class
        {
            await DbSet<T>().AddAsync(entity);
        }

        public async Task AddRangeAsync<T>(IEnumerable<T> entities) where T : class
        {
            await DbSet<T>().AddRangeAsync(entities);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return DbSet<T>();
        }

        public IQueryable<T> AllReadonly<T>() where T : class
        {
            return DbSet<T>().AsNoTracking();
        }

        public async Task<T?> GetByIdAsync<T>(object id) where T : class
        {
            return await DbSet<T>().FindAsync(id);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<bool> IsInvitationValidAsync(string email)
        {
            var currentDateTime = DateTime.UtcNow;

            var existingInvitation = await DbSet<JInvitation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InviteeEmail == email && i.ExpirationDate > currentDateTime && i.IsActive);

            return existingInvitation == null;

        }

        public async Task<bool> AnyUsersAsync()
        {
            return await _context.Users.AnyAsync();
        }
    }

}
