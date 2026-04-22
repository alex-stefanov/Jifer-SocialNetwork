using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface IBlogService
{
    Task<(IReadOnlyList<BlogPost> Posts, int Total)> GetPublishedAsync(int page, int pageSize, int? categoryId = null, CancellationToken ct = default);
    Task<IReadOnlyList<BlogPost>> GetFeaturedAsync(int count = 3, CancellationToken ct = default);
    Task<IReadOnlyList<BlogPost>> GetRecentAsync(int count, Guid? excludeId = null, CancellationToken ct = default);
    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<(IReadOnlyList<BlogPost> Posts, int Total)> GetAllAsync(int page, int pageSize, BlogPostStatus? statusFilter = null, CancellationToken ct = default);
    Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<BlogPost> CreateAsync(BlogPost post, CancellationToken ct = default);
    Task<BlogPost> UpdateAsync(BlogPost post, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    // Clears the featured flag from all other posts before setting this one.
    Task<BlogPost> SetFeaturedAsync(Guid id, CancellationToken ct = default);
    Task<BlogPost> TogglePublishedAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BlogCategory>> GetCategoriesAsync(CancellationToken ct = default);
}
