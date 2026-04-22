using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KazanlakEvents.Web.Controllers;

public class SeoController(IApplicationDbContext db) : Controller
{
    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        AddUrl(sb, "/", "daily", "1.0");
        AddUrl(sb, "/Event", "daily", "0.9");
        AddUrl(sb, "/Event/Calendar", "daily", "0.8");
        AddUrl(sb, "/Blog", "weekly", "0.7");
        AddUrl(sb, "/Donation", "weekly", "0.7");
        AddUrl(sb, "/Sponsor", "monthly", "0.5");

        var events = await db.Events
            .Where(e => e.Status == EventStatus.Published)
            .Select(e => new { e.Slug, e.ModifiedAt })
            .ToListAsync(ct);
        foreach (var ev in events)
            AddUrl(sb, $"/Event/Details?slug={ev.Slug}", "weekly", "0.8", ev.ModifiedAt);

        var posts = await db.BlogPosts
            .Where(p => p.IsPublished)
            .Select(p => new { p.Slug, p.ModifiedAt })
            .ToListAsync(ct);
        foreach (var post in posts)
            AddUrl(sb, $"/Blog/Details?slug={post.Slug}", "monthly", "0.6", post.ModifiedAt);

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var content = $"User-agent: *\nAllow: /\nDisallow: /Admin/\nDisallow: /Account/\nDisallow: /hangfire/\nSitemap: {Request.Scheme}://{Request.Host}/sitemap.xml";
        return Content(content, "text/plain");
    }

    private void AddUrl(StringBuilder sb, string path, string changefreq, string priority, DateTime? lastmod = null)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{Request.Scheme}://{Request.Host}{path}</loc>");
        if (lastmod.HasValue)
            sb.AppendLine($"    <lastmod>{lastmod:yyyy-MM-dd}</lastmod>");
        sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }
}
