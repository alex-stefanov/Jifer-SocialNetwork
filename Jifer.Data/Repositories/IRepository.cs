namespace Jifer.Data.Repositories
{

    public interface IRepository : IDisposable
    {
        IQueryable<T> All<T>() where T : class;

        IQueryable<T> AllReadonly<T>() where T : class;

        Task<T?> GetByIdAsync<T>(object id) where T : class;

        Task AddAsync<T>(T entity) where T : class;

        Task AddRangeAsync<T>(IEnumerable<T> entities) where T : class;

        Task<int> SaveChangesAsync();

        Task<bool> IsInvitationValidAsync(string email);

        Task<bool> AnyUsersAsync();
    }
}
