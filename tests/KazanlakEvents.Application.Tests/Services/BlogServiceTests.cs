using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class BlogServiceTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<ISlugService>          _slug = null!;
    private Mock<IUnitOfWork>           _uow = null!;
    private Mock<IHtmlSanitizerService> _sanitizer = null!;
    private BlogService                 _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db        = new Mock<IApplicationDbContext>();
        _slug      = new Mock<ISlugService>();
        _uow       = new Mock<IUnitOfWork>();
        _sanitizer = new Mock<IHtmlSanitizerService>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sanitizer.Setup(h => h.Sanitize(It.IsAny<string>())).Returns<string>(s => s);

        _sut = new BlogService(_db.Object, _slug.Object, _uow.Object, _sanitizer.Object);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static BlogCategory MakeCategory(int id = 1) =>
        new() { Id = id, Name = "Tech", Slug = "tech", IsActive = true };

    private static BlogPost MakePost(
        BlogPostStatus status = BlogPostStatus.Published,
        bool isFeatured = false,
        int categoryId = 1,
        DateTime? publishedAt = null)
    {
        return new BlogPost
        {
            Id          = Guid.NewGuid(),
            Title       = "Test Post",
            Slug        = $"test-post-{Guid.NewGuid():N}",
            Content     = "<p>content</p>",
            Status      = status,
            IsFeatured  = isFeatured,
            CategoryId  = categoryId,
            PublishedAt = publishedAt,
            AuthorId    = Guid.NewGuid(),
            ViewCount   = 0,
            Category    = MakeCategory(categoryId),
            CreatedAt   = DateTime.UtcNow
        };
    }

    // ── GetPublishedAsync ────────────────────────────────────────────────────

    [Test]
    public async Task GetPublishedAsync_ReturnsOnlyPublishedPosts_Paged()
    {
        var posts = new List<BlogPost>
        {
            MakePost(BlogPostStatus.Published, publishedAt: DateTime.UtcNow.AddDays(-1)),
            MakePost(BlogPostStatus.Published, publishedAt: DateTime.UtcNow.AddDays(-2)),
            MakePost(BlogPostStatus.Draft)
        };
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var (result, total) = await _sut.GetPublishedAsync(page: 1, pageSize: 10);

        Assert.That(total,  Is.EqualTo(2));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetPublishedAsync_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        var posts = new List<BlogPost>
        {
            MakePost(BlogPostStatus.Published, categoryId: 1, publishedAt: DateTime.UtcNow),
            MakePost(BlogPostStatus.Published, categoryId: 2, publishedAt: DateTime.UtcNow),
            MakePost(BlogPostStatus.Published, categoryId: 1, publishedAt: DateTime.UtcNow)
        };
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var (result, total) = await _sut.GetPublishedAsync(1, 10, categoryId: 1);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(result.All(p => p.CategoryId == 1), Is.True);
    }

    [Test]
    public async Task GetPublishedAsync_Page2_ReturnsCorrectSlice()
    {
        var posts = Enumerable.Range(1, 5)
            .Select(i => MakePost(BlogPostStatus.Published, publishedAt: DateTime.UtcNow.AddDays(-i)))
            .ToList();
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var (result, total) = await _sut.GetPublishedAsync(page: 2, pageSize: 2);

        Assert.That(total,  Is.EqualTo(5));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    // ── GetFeaturedAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetFeaturedAsync_ReturnsOnlyFeaturedPublishedPosts()
    {
        var posts = new List<BlogPost>
        {
            MakePost(BlogPostStatus.Published, isFeatured: true,  publishedAt: DateTime.UtcNow.AddDays(-1)),
            MakePost(BlogPostStatus.Published, isFeatured: true,  publishedAt: DateTime.UtcNow.AddDays(-2)),
            MakePost(BlogPostStatus.Published, isFeatured: false, publishedAt: DateTime.UtcNow.AddDays(-3)),
            MakePost(BlogPostStatus.Draft,     isFeatured: true)
        };
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetFeaturedAsync(count: 3);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(p => p.IsFeatured && p.Status == BlogPostStatus.Published), Is.True);
    }

    [Test]
    public async Task GetFeaturedAsync_LimitsToCount()
    {
        var posts = Enumerable.Range(1, 5)
            .Select(i => MakePost(BlogPostStatus.Published, isFeatured: true, publishedAt: DateTime.UtcNow.AddDays(-i)))
            .ToList();
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetFeaturedAsync(count: 2);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    // ── GetRecentAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task GetRecentAsync_ReturnsPublishedPostsUpToCount()
    {
        var posts = Enumerable.Range(1, 4)
            .Select(i => MakePost(BlogPostStatus.Published, publishedAt: DateTime.UtcNow.AddDays(-i)))
            .ToList();
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetRecentAsync(count: 3);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetRecentAsync_ExcludesSpecifiedId()
    {
        var posts = Enumerable.Range(1, 3)
            .Select(i => MakePost(BlogPostStatus.Published, publishedAt: DateTime.UtcNow.AddDays(-i)))
            .ToList();
        var excludeId = posts[0].Id;
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetRecentAsync(count: 10, excludeId: excludeId);

        Assert.That(result.Any(p => p.Id == excludeId), Is.False);
        Assert.That(result, Has.Count.EqualTo(2));
    }

    // ── GetBySlugAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task GetBySlugAsync_ExistingSlug_IncrementsViewCountAndReturnsPost()
    {
        var post = MakePost(BlogPostStatus.Published, publishedAt: DateTime.UtcNow);
        post.Slug = "my-slug";
        post.ViewCount = 5;

        _db.Setup(d => d.BlogPosts).Returns(new List<BlogPost> { post }.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetBySlugAsync("my-slug");

        Assert.That(result,             Is.Not.Null);
        Assert.That(result!.Slug,       Is.EqualTo("my-slug"));
        Assert.That(result.ViewCount,   Is.EqualTo(6));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetBySlugAsync_NonExistentSlug_ReturnsNull()
    {
        _db.Setup(d => d.BlogPosts).Returns(new List<BlogPost>().AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetBySlugAsync("does-not-exist");

        Assert.That(result, Is.Null);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetBySlugAsync_DraftPost_ReturnsNull()
    {
        var draft = MakePost(BlogPostStatus.Draft);
        draft.Slug = "draft-slug";
        _db.Setup(d => d.BlogPosts).Returns(new List<BlogPost> { draft }.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetBySlugAsync("draft-slug");

        Assert.That(result, Is.Null);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_NoFilter_ReturnsAllPostsPaged()
    {
        var posts = new List<BlogPost>
        {
            MakePost(BlogPostStatus.Published),
            MakePost(BlogPostStatus.Draft),
            MakePost(BlogPostStatus.Published)
        };
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var (result, total) = await _sut.GetAllAsync(1, 10);

        Assert.That(total,  Is.EqualTo(3));
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetAllAsync_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var posts = new List<BlogPost>
        {
            MakePost(BlogPostStatus.Published),
            MakePost(BlogPostStatus.Draft),
            MakePost(BlogPostStatus.Draft)
        };
        _db.Setup(d => d.BlogPosts).Returns(posts.AsQueryable().BuildMockDbSet().Object);

        var (result, total) = await _sut.GetAllAsync(1, 10, BlogPostStatus.Draft);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(result.All(p => p.Status == BlogPostStatus.Draft), Is.True);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsPost()
    {
        var post = MakePost(BlogPostStatus.Published);
        _db.Setup(d => d.BlogPosts).Returns(new List<BlogPost> { post }.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetByIdAsync(post.Id);

        Assert.That(result,     Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(post.Id));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        _db.Setup(d => d.BlogPosts).Returns(new List<BlogPost>().AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_GeneratesSlugAndSanitizesContent()
    {
        var posts = new List<BlogPost>();
        var mockSet = posts.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<BlogPost>())).Callback<BlogPost>(p => posts.Add(p));
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);
        _slug.Setup(s => s.GenerateUniqueSlugAsync<BlogPost>(
            It.IsAny<string>(), It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("generated-slug");

        var newPost = new BlogPost
        {
            Title   = "My New Post",
            Content = "<script>bad</script>safe",
            Status  = BlogPostStatus.Draft,
            AuthorId = Guid.NewGuid()
        };

        var result = await _sut.CreateAsync(newPost);

        Assert.That(result.Slug,    Is.EqualTo("generated-slug"));
        _sanitizer.Verify(h => h.Sanitize(It.IsAny<string>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_PublishedStatus_SetsPublishedAt()
    {
        var posts = new List<BlogPost>();
        var mockSet = posts.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<BlogPost>())).Callback<BlogPost>(p => posts.Add(p));
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);
        _slug.Setup(s => s.GenerateUniqueSlugAsync<BlogPost>(
            It.IsAny<string>(), It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pub-slug");

        var newPost = new BlogPost
        {
            Title   = "Published Post",
            Content = "Content",
            Status  = BlogPostStatus.Published,
            AuthorId = Guid.NewGuid()
        };

        var result = await _sut.CreateAsync(newPost);

        Assert.That(result.PublishedAt, Is.Not.Null);
    }

    [Test]
    public async Task CreateAsync_DraftStatus_DoesNotSetPublishedAt()
    {
        var posts = new List<BlogPost>();
        var mockSet = posts.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<BlogPost>())).Callback<BlogPost>(p => posts.Add(p));
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);
        _slug.Setup(s => s.GenerateUniqueSlugAsync<BlogPost>(
            It.IsAny<string>(), It.IsAny<Func<string, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("draft-slug");

        var newPost = new BlogPost
        {
            Title   = "Draft Post",
            Content = "Content",
            Status  = BlogPostStatus.Draft,
            AuthorId = Guid.NewGuid()
        };

        var result = await _sut.CreateAsync(newPost);

        Assert.That(result.PublishedAt, Is.Null);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ExistingPost_UpdatesFieldsAndSaves()
    {
        var existing = MakePost(BlogPostStatus.Draft);
        var mockSet = new List<BlogPost> { existing }.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(existing);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        var updated = new BlogPost
        {
            Id         = existing.Id,
            Title      = "Updated Title",
            Content    = "Updated Content",
            Excerpt    = "Updated Excerpt",
            CategoryId = 2,
            IsFeatured = true,
            Status     = BlogPostStatus.Draft
        };

        var result = await _sut.UpdateAsync(updated);

        Assert.That(result.Title,      Is.EqualTo("Updated Title"));
        Assert.That(result.Excerpt,    Is.EqualTo("Updated Excerpt"));
        Assert.That(result.IsFeatured, Is.True);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_TransitionToPublished_SetsPublishedAt()
    {
        var existing = MakePost(BlogPostStatus.Draft);
        var mockSet = new List<BlogPost> { existing }.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(existing);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        var updated = new BlogPost
        {
            Id     = existing.Id,
            Title  = existing.Title,
            Content = existing.Content,
            Status = BlogPostStatus.Published
        };

        var result = await _sut.UpdateAsync(updated);

        Assert.That(result.Status,      Is.EqualTo(BlogPostStatus.Published));
        Assert.That(result.PublishedAt, Is.Not.Null);
    }

    [Test]
    public async Task UpdateAsync_PostNotFound_ThrowsInvalidOperationException()
    {
        var mockSet = new List<BlogPost>().AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((BlogPost?)null);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(new BlogPost { Id = Guid.NewGuid() }));

        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ExistingPost_RemovesAndSaves()
    {
        var post = MakePost();
        var posts = new List<BlogPost> { post };
        var mockSet = posts.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(post);
        mockSet.Setup(s => s.Remove(It.IsAny<BlogPost>())).Callback<BlogPost>(p => posts.Remove(p));
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        await _sut.DeleteAsync(post.Id);

        Assert.That(posts, Is.Empty);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_PostNotFound_ThrowsInvalidOperationException()
    {
        var mockSet = new List<BlogPost>().AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((BlogPost?)null);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    // ── TogglePublishedAsync ─────────────────────────────────────────────────

    [Test]
    public async Task TogglePublishedAsync_PublishedPost_SetsStatusToDraft()
    {
        var post = MakePost(BlogPostStatus.Published, publishedAt: DateTime.UtcNow);
        var mockSet = new List<BlogPost> { post }.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(post);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        var result = await _sut.TogglePublishedAsync(post.Id);

        Assert.That(result.Status, Is.EqualTo(BlogPostStatus.Draft));
    }

    [Test]
    public async Task TogglePublishedAsync_DraftPost_SetsStatusToPublishedAndSetsPublishedAt()
    {
        var post = MakePost(BlogPostStatus.Draft);
        var mockSet = new List<BlogPost> { post }.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(post);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        var result = await _sut.TogglePublishedAsync(post.Id);

        Assert.That(result.Status,      Is.EqualTo(BlogPostStatus.Published));
        Assert.That(result.PublishedAt, Is.Not.Null);
    }

    [Test]
    public async Task TogglePublishedAsync_AlreadyPublishedWithPublishedAt_DoesNotOverridePublishedAt()
    {
        var original = DateTime.UtcNow.AddDays(-5);
        var post = MakePost(BlogPostStatus.Draft);
        post.PublishedAt = original;
        var mockSet = new List<BlogPost> { post }.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(post);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        var result = await _sut.TogglePublishedAsync(post.Id);

        // When transitioning from Draft→Published and PublishedAt is already set, keep original
        // (the code checks `if (post.PublishedAt == null)`)
        Assert.That(result.PublishedAt, Is.EqualTo(original));
    }

    [Test]
    public async Task TogglePublishedAsync_PostNotFound_ThrowsInvalidOperationException()
    {
        var mockSet = new List<BlogPost>().AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((BlogPost?)null);
        _db.Setup(d => d.BlogPosts).Returns(mockSet.Object);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.TogglePublishedAsync(Guid.NewGuid()));
    }

    // ── GetCategoriesAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetCategoriesAsync_ReturnsOnlyActiveCategories_OrderedByName()
    {
        var categories = new List<BlogCategory>
        {
            new() { Id = 1, Name = "Zebra",  Slug = "z", IsActive = true },
            new() { Id = 2, Name = "Alpha",  Slug = "a", IsActive = true },
            new() { Id = 3, Name = "Inactive", Slug = "i", IsActive = false }
        };
        _db.Setup(d => d.BlogCategories).Returns(categories.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetCategoriesAsync();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Alpha"));
        Assert.That(result[1].Name, Is.EqualTo("Zebra"));
    }
}
