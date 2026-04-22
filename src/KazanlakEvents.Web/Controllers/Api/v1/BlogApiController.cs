using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Web.Controllers.Api;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KazanlakEvents.Web.Controllers.Api.v1;

/// <summary>
/// Blog posts: public browsing, authoring, and moderation workflow.
/// </summary>
[Route("api/v1/blog")]
public class BlogApiController(
    IBlogService blogService,
    ICurrentUserService currentUser) : BaseApiController
{

    /// <summary>Get a paginated list of published blog posts with optional category filter.</summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (1–50)</param>
    /// <param name="categoryId">Filter by blog category ID</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResult<BlogPostApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var (posts, total) = await blogService.GetPublishedAsync(page, pageSize, categoryId, ct);

        return Ok(new ApiPagedResult<BlogPostApiDto>
        {
            Items      = posts.Select(MapToDto).ToList(),
            Page       = page,
            PageSize   = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>Get a single published blog post by its GUID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BlogPostDetailApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostById(Guid id, CancellationToken ct = default)
    {
        var post = await blogService.GetByIdAsync(id, ct);
        if (post is null || (!post.IsPublished && !IsAdminOrAuthor(post)))
            return NotFound(new { error = "Post not found" });

        return Ok(MapToDetailDto(post));
    }

    /// <summary>Get a single published blog post by its URL slug.</summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(BlogPostDetailApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostBySlug(string slug, CancellationToken ct = default)
    {
        var post = await blogService.GetBySlugAsync(slug, ct);
        if (post is null || (!post.IsPublished && !IsAdminOrAuthor(post)))
            return NotFound(new { error = "Post not found" });

        return Ok(MapToDetailDto(post));
    }


    /// <summary>Create a new blog post draft. Requires BlogAuthor role or higher.</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "BlogAuthor,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(BlogPostDetailApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreateBlogPostApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var post = new BlogPost
            {
                Title         = request.Title,
                Content       = request.Content,
                Excerpt       = request.Excerpt,
                CoverImageUrl = request.CoverImageUrl,
                CategoryId    = request.CategoryId,
                AuthorId      = currentUser.UserId!.Value,
                Status        = BlogPostStatus.Draft,
                IsFeatured    = false
            };

            var created = await blogService.CreateAsync(post, ct);
            return CreatedAtAction(nameof(GetPostById), new { id = created.Id }, MapToDetailDto(created));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Update an existing blog post. Author or Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "BlogAuthor,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(BlogPostDetailApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost(
        Guid id,
        [FromBody] UpdateBlogPostApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var post = await blogService.GetByIdAsync(id, ct);
            if (post is null) return NotFound(new { error = "Post not found" });

            if (!IsAdminOrAuthor(post)) return Forbid();

            post.Title         = request.Title;
            post.Content       = request.Content;
            post.Excerpt       = request.Excerpt;
            post.CoverImageUrl = request.CoverImageUrl;
            post.CategoryId    = request.CategoryId;

            var updated = await blogService.UpdateAsync(post, ct);
            return Ok(MapToDetailDto(updated));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Delete a blog post. Author or Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "BlogAuthor,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken ct = default)
    {
        try
        {
            var post = await blogService.GetByIdAsync(id, ct);
            if (post is null) return NotFound(new { error = "Post not found" });

            if (!IsAdminOrAuthor(post)) return Forbid();

            await blogService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    /// <summary>Publish / unpublish a blog post (toggle). Admin only.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(BlogPostDetailApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApprovePost(Guid id, CancellationToken ct = default)
    {
        try
        {
            var post = await blogService.TogglePublishedAsync(id, ct);
            return Ok(MapToDetailDto(post));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Reject / revert a blog post to Draft status. Admin only.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(BlogPostDetailApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectPost(Guid id, CancellationToken ct = default)
    {
        try
        {
            var post = await blogService.GetByIdAsync(id, ct);
            if (post is null) return NotFound(new { error = "Post not found" });

            post.Status      = BlogPostStatus.Draft;
            post.PublishedAt = null;

            var updated = await blogService.UpdateAsync(post, ct);
            return Ok(MapToDetailDto(updated));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    private bool IsAdminOrAuthor(BlogPost post)
        => post.AuthorId == currentUser.UserId
           || currentUser.IsInRole("Admin")
           || currentUser.IsInRole("SuperAdmin");

    private static BlogPostApiDto MapToDto(BlogPost p) => new()
    {
        Id           = p.Id,
        Title        = p.Title,
        Slug         = p.Slug,
        Excerpt      = p.Excerpt,
        CoverImageUrl = p.CoverImageUrl,
        CategoryName = p.Category?.Name,
        IsFeatured   = p.IsFeatured,
        Status       = p.Status.ToString(),
        PublishedAt  = p.PublishedAt,
        ViewCount    = p.ViewCount
    };

    private static BlogPostDetailApiDto MapToDetailDto(BlogPost p) => new()
    {
        Id           = p.Id,
        Title        = p.Title,
        Slug         = p.Slug,
        Excerpt      = p.Excerpt,
        CoverImageUrl = p.CoverImageUrl,
        CategoryName = p.Category?.Name,
        IsFeatured   = p.IsFeatured,
        Status       = p.Status.ToString(),
        PublishedAt  = p.PublishedAt,
        ViewCount    = p.ViewCount,
        Content      = p.Content,
        AuthorId     = p.AuthorId
    };
}
