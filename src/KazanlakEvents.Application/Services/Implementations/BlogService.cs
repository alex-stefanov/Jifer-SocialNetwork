using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Application.Services.Implementations;

public class BlogService(
    IApplicationDbContext db,
    ISlugService slugService,
    IUnitOfWork unitOfWork,
    IHtmlSanitizerService htmlSanitizer) : IBlogService
{
    public async Task<(IReadOnlyList<BlogPost> Posts, int Total)> GetPublishedAsync(
        int page, int pageSize, int? categoryId = null, CancellationToken ct = default)
    {
        var query = db.BlogPosts
            .Include(p => p.Category)
            .Where(p => p.Status == BlogPostStatus.Published);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        var total = await query.CountAsync(ct);
        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (posts, total);
    }

    public async Task<IReadOnlyList<BlogPost>> GetFeaturedAsync(int count = 3, CancellationToken ct = default)
        => await db.BlogPosts
            .Include(p => p.Category)
            .Where(p => p.Status == BlogPostStatus.Published && p.IsFeatured)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BlogPost>> GetRecentAsync(int count, Guid? excludeId = null, CancellationToken ct = default)
        => await db.BlogPosts
            .Include(p => p.Category)
            .Where(p => p.Status == BlogPostStatus.Published && (excludeId == null || p.Id != excludeId))
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var post = await db.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Tags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == BlogPostStatus.Published, ct);

        if (post != null)
        {
            post.ViewCount++;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return post;
    }

    public async Task<(IReadOnlyList<BlogPost> Posts, int Total)> GetAllAsync(
        int page, int pageSize, BlogPostStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = db.BlogPosts
            .Include(p => p.Category)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(p => p.Status == statusFilter.Value);

        var total = await query.CountAsync(ct);
        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (posts, total);
    }

    public async Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.BlogPosts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<BlogPost> CreateAsync(BlogPost post, CancellationToken ct = default)
    {
        post.Slug = await slugService.GenerateUniqueSlugAsync<BlogPost>(
            post.Title,
            async s => await db.BlogPosts.AnyAsync(p => p.Slug == s, ct),
            ct);

        post.Content = htmlSanitizer.Sanitize(post.Content);

        if (post.Status == BlogPostStatus.Published && post.PublishedAt == null)
            post.PublishedAt = DateTime.UtcNow;

        db.BlogPosts.Add(post);
        await unitOfWork.SaveChangesAsync(ct);
        return post;
    }

    public async Task<BlogPost> UpdateAsync(BlogPost post, CancellationToken ct = default)
    {
        var existing = await db.BlogPosts.FindAsync(new object[] { post.Id }, ct)
            ?? throw new InvalidOperationException("Blog post not found.");

        existing.Title         = post.Title;
        existing.Content       = htmlSanitizer.Sanitize(post.Content);
        existing.Excerpt       = post.Excerpt;
        existing.CoverImageUrl = post.CoverImageUrl;
        existing.CategoryId    = post.CategoryId;
        existing.IsFeatured    = post.IsFeatured;

        if (existing.Status != BlogPostStatus.Published && post.Status == BlogPostStatus.Published)
            existing.PublishedAt = DateTime.UtcNow;

        existing.Status = post.Status;

        await unitOfWork.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var post = await db.BlogPosts.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException("Blog post not found.");

        db.BlogPosts.Remove(post);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<BlogPost> SetFeaturedAsync(Guid id, CancellationToken ct = default)
    {
        await db.BlogPosts
            .Where(p => p.Id != id && p.IsFeatured)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsFeatured, false), ct);

        var post = await db.BlogPosts.FindAsync(new object?[] { id }, ct)
            ?? throw new InvalidOperationException("Blog post not found.");

        post.IsFeatured = true;
        await unitOfWork.SaveChangesAsync(ct);
        return post;
    }

    public async Task<BlogPost> TogglePublishedAsync(Guid id, CancellationToken ct = default)
    {
        var post = await db.BlogPosts.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException("Blog post not found.");

        if (post.Status == BlogPostStatus.Published)
        {
            post.Status = BlogPostStatus.Draft;
        }
        else
        {
            post.Status = BlogPostStatus.Published;
            if (post.PublishedAt == null)
                post.PublishedAt = DateTime.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return post;
    }

    public async Task<IReadOnlyList<BlogCategory>> GetCategoriesAsync(CancellationToken ct = default)
        => await db.BlogCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
}
