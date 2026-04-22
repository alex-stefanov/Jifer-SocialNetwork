using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Web.Resources;
using KazanlakEvents.Web.ViewModels.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace KazanlakEvents.Web.Controllers;

public class BlogController(
    IBlogService blogService,
    ICurrentUserService currentUser,
    IApplicationDbContext db,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    private const int PageSize = 9;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int? categoryId = null, CancellationToken ct = default)
    {
        var (posts, total) = await blogService.GetPublishedAsync(page, PageSize, categoryId, ct);
        var categories     = await blogService.GetCategoriesAsync(ct);

        BlogPostCardViewModel? featuredPost = null;
        IReadOnlyList<BlogPostCardViewModel> recentPosts = new List<BlogPostCardViewModel>();

        if (page == 1 && categoryId == null)
        {
            var featuredList = await blogService.GetFeaturedAsync(1, ct);
            if (!featuredList.Any())
                featuredList = await blogService.GetRecentAsync(1, null, ct);

            featuredPost = featuredList.Select(p => MapCard(p, null)).FirstOrDefault();
            var recentRaw = await blogService.GetRecentAsync(3, featuredPost?.Id, ct);
            recentPosts = recentRaw.Select(p => MapCard(p, null)).ToList();
        }

        var vm = new BlogIndexViewModel
        {
            Posts          = posts.Select(p => MapCard(p, null)).ToList(),
            Categories     = categories.Select(c => new BlogCategoryViewModel
            {
                Id          = c.Id,
                Name        = c.Name,
                Slug        = c.Slug,
                Description = c.Description
            }).ToList(),
            FeaturedPost   = featuredPost,
            RecentPosts    = recentPosts,
            CategoryFilter = categoryId,
            CurrentPage    = page,
            TotalPages     = (int)Math.Ceiling(total / (double)PageSize),
            TotalPosts     = total
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string slug, CancellationToken ct = default)
    {
        var post = await blogService.GetBySlugAsync(slug, ct);
        if (post == null) return NotFound();

        var relatedRaw = await blogService.GetRecentAsync(3, post.Id, ct);

        var vm = new BlogPostDetailViewModel
        {
            Id            = post.Id,
            Title         = post.Title,
            Slug          = post.Slug,
            Content       = post.Content,
            Excerpt       = post.Excerpt,
            CoverImageUrl = post.CoverImageUrl,
            CategoryName  = post.Category?.Name,
            CategoryId    = post.CategoryId,
            IsFeatured    = post.IsFeatured,
            PublishedAt   = post.PublishedAt,
            ViewCount     = post.ViewCount,
            Tags          = post.Tags.Select(t => t.Tag.Name).ToList(),
            ReadTimeMinutes = EstimateReadTime(post.Content),
            RelatedPosts  = relatedRaw.Select(p => MapCard(p, null)).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize]
    public IActionResult RequestAuthor() => View();

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAuthor(string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
        {
            ModelState.AddModelError("reason", localizer["ReasonRequired"].Value);
            return View();
        }

        var userId = currentUser.UserId!.Value;

        var existing = await db.BlogAuthorRequests
            .AnyAsync(r => r.UserId == userId && !r.IsReviewed, ct);
        if (existing)
        {
            TempData["Error"] = localizer["BlogAuthorRequestAlreadyPending"].Value;
            return RedirectToAction("Index", "Profile");
        }

        db.BlogAuthorRequests.Add(new BlogAuthorRequest
        {
            UserId = userId,
            Reason = reason
        });
        await db.SaveChangesAsync(ct);

        TempData["Success"] = localizer["BlogAuthorRequestSubmitted"].Value;
        return RedirectToAction("Index", "Profile");
    }

    [HttpGet]
    [Authorize(Roles = "Moderator,Admin,SuperAdmin")]
    public IActionResult Manage(int page = 1, string? status = null)
        => RedirectToAction("BlogPosts", "Admin", new { page, status });

    [HttpGet]
    [Authorize(Roles = "BlogAuthor")]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        var vm = new BlogPostCreateViewModel();
        await PopulateCategoriesAsync(vm.Categories, null, ct);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "BlogAuthor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogPostCreateViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.Categories, model.CategoryId, ct);
            return View(model);
        }

        var userId = currentUser.UserId!.Value;

        var profile = await db.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId, ct);
        var isTrusted = profile?.IsTrustedAuthor ?? false;

        var status = isTrusted ? BlogPostStatus.Published : BlogPostStatus.PendingReview;

        var post = new BlogPost
        {
            Title         = model.Title,
            Content       = model.Content,
            Excerpt       = model.Excerpt,
            CoverImageUrl = model.CoverImageUrl,
            CategoryId    = model.CategoryId,
            Status        = status,
            IsFeatured    = false,
            AuthorId      = userId
        };

        await blogService.CreateAsync(post, ct);
        TempData["Success"] = isTrusted
            ? localizer["BlogPostCreated"].Value
            : localizer["BlogPostSubmittedForReview"].Value;

        return RedirectToAction(nameof(MyPosts));
    }

    [HttpGet]
    [Authorize(Roles = "BlogAuthor")]
    public async Task<IActionResult> MyPosts(int page = 1, CancellationToken ct = default)
    {
        var userId = currentUser.UserId!.Value;

        var query = db.BlogPosts
            .Include(p => p.Category)
            .Where(p => p.AuthorId == userId)
            .OrderByDescending(p => p.CreatedAt);

        var total   = await query.CountAsync(ct);
        var myPosts = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        var vm = new BlogIndexViewModel
        {
            Posts       = myPosts.Select(p => MapCard(p, null)).ToList(),
            CurrentPage = page,
            TotalPages  = (int)Math.Ceiling(total / (double)PageSize),
            TotalPosts  = total
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "BlogAuthor,Moderator,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        var post = await blogService.GetByIdAsync(id, ct);
        if (post == null) return NotFound();

        if (User.IsInRole("BlogAuthor") && !User.IsInRole("Moderator") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            if (post.AuthorId != currentUser.UserId)
                return Forbid();
        }

        var vm = new BlogPostEditViewModel
        {
            Id            = post.Id,
            Title         = post.Title,
            Content       = post.Content,
            Excerpt       = post.Excerpt,
            CoverImageUrl = post.CoverImageUrl,
            CategoryId    = post.CategoryId,
            IsPublished   = post.IsPublished,
            IsFeatured    = post.IsFeatured
        };

        await PopulateCategoriesAsync(vm.Categories, vm.CategoryId, ct);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "BlogAuthor,Moderator,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogPostEditViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.Categories, model.CategoryId, ct);
            return View(model);
        }

        var existing = await blogService.GetByIdAsync(model.Id, ct);
        if (existing == null) return NotFound();

        if (User.IsInRole("BlogAuthor") && !User.IsInRole("Moderator") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            if (existing.AuthorId != currentUser.UserId)
                return Forbid();
        }

        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("Moderator");
        var newStatus = isPrivileged
            ? (model.IsPublished ? BlogPostStatus.Published : existing.Status)
            : existing.Status;

        var post = new BlogPost
        {
            Id            = model.Id,
            Title         = model.Title,
            Content       = model.Content,
            Excerpt       = model.Excerpt,
            CoverImageUrl = model.CoverImageUrl,
            CategoryId    = model.CategoryId,
            Status        = newStatus,
            IsFeatured    = isPrivileged && model.IsFeatured
        };

        await blogService.UpdateAsync(post, ct);
        TempData["Success"] = localizer["BlogPostUpdated"].Value;

        return isPrivileged
            ? RedirectToAction(nameof(Manage))
            : RedirectToAction(nameof(MyPosts));
    }

    [HttpPost]
    [Authorize(Roles = "BlogAuthor,Moderator,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await blogService.DeleteAsync(id, ct);
        TempData["Success"] = localizer["BlogPostDeleted"].Value;
        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [Authorize(Roles = "Moderator,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetFeatured(Guid id, CancellationToken ct = default)
    {
        await blogService.SetFeaturedAsync(id, ct);
        return RedirectToAction(nameof(Manage), new { status = Request.Form["returnStatus"].ToString() });
    }

    [HttpPost]
    [Authorize(Roles = "Moderator,Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublished(Guid id, CancellationToken ct = default)
    {
        await blogService.TogglePublishedAsync(id, ct);
        return RedirectToAction(nameof(Manage));
    }

    private static BlogPostCardViewModel MapCard(BlogPost p, Dictionary<Guid, string>? authorNames) => new()
    {
        Id              = p.Id,
        Title           = p.Title,
        Slug            = p.Slug,
        Excerpt         = p.Excerpt,
        CoverImageUrl   = p.CoverImageUrl,
        CategoryName    = p.Category?.Name,
        CategoryId      = p.CategoryId,
        IsFeatured      = p.IsFeatured,
        Status          = p.Status,
        PublishedAt     = p.PublishedAt,
        ViewCount       = p.ViewCount,
        AuthorName      = authorNames != null && authorNames.TryGetValue(p.AuthorId, out var n) ? n : null,
        ReadTimeMinutes = EstimateReadTime(p.Content)
    };

    private static int EstimateReadTime(string? content)
    {
        if (string.IsNullOrEmpty(content)) return 1;
        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
    }

    private async Task PopulateCategoriesAsync(
        List<SelectListItem> list, int? selectedId, CancellationToken ct)
    {
        var cats = await blogService.GetCategoriesAsync(ct);
        list.Add(new SelectListItem { Value = "", Text = "— None —" });
        list.AddRange(cats.Select(c => new SelectListItem
        {
            Value    = c.Id.ToString(),
            Text     = c.Name,
            Selected = c.Id == selectedId
        }));
    }
}
