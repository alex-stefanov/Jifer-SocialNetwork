using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.ViewModels.About;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace KazanlakEvents.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class TeamController(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment env) : Controller
{
    [HttpGet]
    public async Task<IActionResult> ManageTeam(Guid organizedEventId, CancellationToken ct = default)
    {
        var ev = await db.OrganizedEvents
            .Include(e => e.TeamMembers)
            .FirstOrDefaultAsync(e => e.Id == organizedEventId, ct);

        if (ev == null) return NotFound();

        ViewBag.ActivePage = "OrganizedEvents";

        var vm = new ManageTeamViewModel
        {
            OrganizedEventId = ev.Id,
            EventTitle       = ev.Title,
            TeamMembers      = ev.TeamMembers
                .OrderBy(t => t.Role).ThenBy(t => t.SortOrder)
                .Select(t => new TeamMemberViewModel
                {
                    Id           = t.Id,
                    FullName     = t.FullName,
                    PhotoUrl     = t.PhotoUrl,
                    Description  = t.Description,
                    Quote        = t.Quote,
                    Role         = t.Role,
                    TagList      = string.IsNullOrEmpty(t.Tags) ? [] : t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    InstagramUrl = t.InstagramUrl,
                    EmailAddress = t.EmailAddress,
                    LinkedUserId = t.LinkedUserId,
                    SortOrder    = t.SortOrder
                }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTeamMember(
        Guid organizedEventId,
        string fullName,
        string role,
        string? tags,
        string? description,
        string? quote,
        string? instagramUrl,
        string? emailAddress,
        IFormFile? photo,
        CancellationToken ct = default)
    {
        var ev = await db.OrganizedEvents.FirstOrDefaultAsync(e => e.Id == organizedEventId, ct);
        if (ev == null) return NotFound();

        string? photoUrl = null;
        if (photo != null && photo.Length > 0)
        {
            var ext      = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowed  = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Only JPG, PNG and WebP images are allowed.";
                return RedirectToAction(nameof(ManageTeam), new { organizedEventId });
            }
            if (photo.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Image must be under 5 MB.";
                return RedirectToAction(nameof(ManageTeam), new { organizedEventId });
            }

            var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "team");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using var stream = System.IO.File.Create(filePath);
            await photo.CopyToAsync(stream, ct);
            photoUrl = $"/uploads/team/{fileName}";
        }

        var maxOrder = await db.TeamMembers
            .Where(t => t.OrganizedEventId == organizedEventId)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync(ct) ?? 0;

        db.TeamMembers.Add(new TeamMember
        {
            OrganizedEventId = organizedEventId,
            FullName         = fullName,
            Role             = role,
            Tags             = tags,
            Description      = description,
            Quote            = quote,
            InstagramUrl     = instagramUrl,
            EmailAddress     = emailAddress,
            PhotoUrl         = photoUrl,
            SortOrder        = maxOrder + 1
        });

        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Team member added.";
        return RedirectToAction(nameof(ManageTeam), new { organizedEventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeamMember(Guid teamMemberId, Guid organizedEventId, CancellationToken ct = default)
    {
        var member = await db.TeamMembers.FirstOrDefaultAsync(t => t.Id == teamMemberId, ct);
        if (member != null)
        {
            db.TeamMembers.Remove(member);
            await db.SaveChangesAsync(ct);
        }
        TempData["Success"] = "Team member removed.";
        return RedirectToAction(nameof(ManageTeam), new { organizedEventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkTeamMemberToAccount(Guid teamMemberId, string userEmail, Guid organizedEventId, CancellationToken ct = default)
    {
        var member = await db.TeamMembers.FirstOrDefaultAsync(t => t.Id == teamMemberId, ct);
        if (member == null) return NotFound();

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(ManageTeam), new { organizedEventId });
        }

        member.LinkedUserId = user.Id;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Team member linked to {userEmail}.";
        return RedirectToAction(nameof(ManageTeam), new { organizedEventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkTeamMember(Guid teamMemberId, Guid organizedEventId, CancellationToken ct = default)
    {
        var member = await db.TeamMembers.FirstOrDefaultAsync(t => t.Id == teamMemberId, ct);
        if (member == null) return NotFound();

        member.LinkedUserId = null;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Account link removed.";
        return RedirectToAction(nameof(ManageTeam), new { organizedEventId });
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewBag.ActivePage = "OrganizedEvents";

        var events = await db.OrganizedEvents
            .Include(e => e.TeamMembers)
            .Include(e => e.Sponsors)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync(ct);

        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        ViewBag.ActivePage = "OrganizedEvents";
        return View(new OrganizedEventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(
        OrganizedEventFormViewModel vm,
        IFormFile? coverImage,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ActivePage = "OrganizedEvents";
            return View(vm);
        }

        var slug = await GenerateUniqueSlug(vm.Title, null, ct);

        string? coverUrl = await UploadCoverAsync(coverImage, ct);

        var ev = new OrganizedEvent
        {
            Title          = vm.Title,
            Slug           = slug,
            Description    = vm.Description,
            CoverImageUrl  = coverUrl,
            EventDate      = vm.EventDate,
            AttendeesCount = vm.AttendeesCount,
            IsActive       = vm.IsActive,
            CreatedAt      = DateTime.UtcNow,
            ModifiedAt     = DateTime.UtcNow
        };

        db.OrganizedEvents.Add(ev);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Organized event created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(Guid id, CancellationToken ct = default)
    {
        ViewBag.ActivePage = "OrganizedEvents";

        var ev = await db.OrganizedEvents.FindAsync(new object[] { id }, ct);
        if (ev == null) return NotFound();

        return View(new OrganizedEventFormViewModel
        {
            Id             = ev.Id,
            Title          = ev.Title,
            Description    = ev.Description,
            CoverImageUrl  = ev.CoverImageUrl,
            EventDate      = ev.EventDate,
            AttendeesCount = ev.AttendeesCount,
            IsActive       = ev.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(
        OrganizedEventFormViewModel vm,
        IFormFile? coverImage,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ActivePage = "OrganizedEvents";
            return View(vm);
        }

        var ev = await db.OrganizedEvents.FindAsync(new object[] { vm.Id! }, ct);
        if (ev == null) return NotFound();

        ev.Title          = vm.Title;
        ev.Description    = vm.Description;
        ev.EventDate      = vm.EventDate;
        ev.AttendeesCount = vm.AttendeesCount;
        ev.IsActive       = vm.IsActive;
        ev.ModifiedAt     = DateTime.UtcNow;

        if (coverImage != null && coverImage.Length > 0)
        {
            var newUrl = await UploadCoverAsync(coverImage, ct);
            if (newUrl != null) ev.CoverImageUrl = newUrl;
        }

        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Organized event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct = default)
    {
        var ev = await db.OrganizedEvents
            .Include(e => e.TeamMembers)
            .Include(e => e.Sponsors)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (ev != null)
        {
            db.OrganizedEvents.Remove(ev);
            await db.SaveChangesAsync(ct);
        }

        TempData["Success"] = "Organized event deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateUniqueSlug(string title, Guid? excludeId, CancellationToken ct)
    {
        var baseSlug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9\s-]", "")
                            .Trim()
                            .Replace(' ', '-');
        baseSlug = Regex.Replace(baseSlug, @"-+", "-");

        var slug  = baseSlug;
        var count = 1;
        while (await db.OrganizedEvents.AnyAsync(e => e.Slug == slug && e.Id != excludeId, ct))
            slug = $"{baseSlug}-{count++}";

        return slug;
    }

    private async Task<string?> UploadCoverAsync(IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return null;

        var ext     = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext)) return null;
        if (file.Length > 8 * 1024 * 1024) return null;

        var dir  = Path.Combine(env.WebRootPath, "uploads", "organized-events");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(dir, name);
        await using var s = System.IO.File.Create(path);
        await file.CopyToAsync(s, ct);
        return $"/uploads/organized-events/{name}";
    }
}
