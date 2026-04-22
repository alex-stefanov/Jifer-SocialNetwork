using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Web.Resources;
using KazanlakEvents.Web.ViewModels.Sponsor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace KazanlakEvents.Web.Controllers;

public class SponsorController(
    ISponsorService sponsorService,
    IFileStorageService fileStorage,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var sponsors = await sponsorService.GetActiveSponsorsAsync(ct);

        var vm = new SponsorIndexViewModel
        {
            Gold      = MapList(sponsors.Where(s => s.Tier == SponsorTier.Gold)),
            Silver    = MapList(sponsors.Where(s => s.Tier == SponsorTier.Silver)),
            Bronze    = MapList(sponsors.Where(s => s.Tier == SponsorTier.Bronze)),
            Community = MapList(sponsors.Where(s => s.Tier == SponsorTier.Community))
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Click(Guid sponsorId, CancellationToken ct = default)
    {
        var sponsor = await sponsorService.GetByIdAsync(sponsorId, ct);
        if (sponsor == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(sponsor.WebsiteUrl))
            return Redirect(sponsor.WebsiteUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EventSponsorClick(Guid eventId, Guid sponsorId, CancellationToken ct = default)
    {
        var sponsor = await sponsorService.GetByIdAsync(sponsorId, ct);
        if (sponsor == null) return NotFound();

        await sponsorService.IncrementClickAsync(eventId, sponsorId, ct);

        if (!string.IsNullOrWhiteSpace(sponsor.WebsiteUrl))
            return Redirect(sponsor.WebsiteUrl);

        return RedirectToAction("Details", "Event");
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult Manage()
        => RedirectToAction("Sponsors", "Admin");

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult Create()
    {
        var vm = new SponsorFormViewModel();
        PopulateTiers(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SponsorFormViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { PopulateTiers(model); return View(model); }

        string? logoUrl = null;
        if (model.LogoFile != null && model.LogoFile.Length > 0)
            logoUrl = await UploadLogoAsync(model.LogoFile, ct);

        var sponsor = new Sponsor
        {
            Name        = model.Name,
            LogoUrl     = logoUrl,
            WebsiteUrl  = model.WebsiteUrl,
            Description = model.Description,
            Tier        = model.Tier,
            IsActive    = model.IsActive
        };

        await sponsorService.CreateAsync(sponsor, ct);
        TempData["Success"] = localizer["SponsorCreated"].Value;
        return RedirectToAction(nameof(Manage));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        var sponsor = await sponsorService.GetByIdAsync(id, ct);
        if (sponsor == null) return NotFound();

        var vm = new SponsorFormViewModel
        {
            Id              = sponsor.Id,
            Name            = sponsor.Name,
            WebsiteUrl      = sponsor.WebsiteUrl,
            Description     = sponsor.Description,
            Tier            = sponsor.Tier,
            IsActive        = sponsor.IsActive,
            ExistingLogoUrl = sponsor.LogoUrl
        };
        PopulateTiers(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SponsorFormViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) { PopulateTiers(model); return View(model); }

        var existing = await sponsorService.GetByIdAsync(model.Id!.Value, ct);
        if (existing == null) return NotFound();

        string? logoUrl = existing.LogoUrl;
        if (model.LogoFile != null && model.LogoFile.Length > 0)
        {
            if (!string.IsNullOrEmpty(existing.LogoUrl))
                await fileStorage.DeleteAsync(existing.LogoUrl, ct);
            logoUrl = await UploadLogoAsync(model.LogoFile, ct);
        }

        existing.Name        = model.Name;
        existing.LogoUrl     = logoUrl;
        existing.WebsiteUrl  = model.WebsiteUrl;
        existing.Description = model.Description;
        existing.Tier        = model.Tier;
        existing.IsActive    = model.IsActive;

        await sponsorService.UpdateAsync(existing, ct);
        TempData["Success"] = localizer["SponsorUpdated"].Value;
        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var sponsor = await sponsorService.GetByIdAsync(id, ct);
        if (sponsor != null && !string.IsNullOrEmpty(sponsor.LogoUrl))
            await fileStorage.DeleteAsync(sponsor.LogoUrl, ct);

        await sponsorService.DeleteAsync(id, ct);
        TempData["Success"] = localizer["SponsorDeleted"].Value;
        return RedirectToAction(nameof(Manage));
    }

    private static IReadOnlyList<SponsorViewModel> MapList(IEnumerable<Sponsor> sponsors)
        => sponsors.Select(s => new SponsorViewModel
        {
            Id          = s.Id,
            Name        = s.Name,
            LogoUrl     = s.LogoUrl,
            WebsiteUrl  = s.WebsiteUrl,
            Description = s.Description,
            Tier        = s.Tier,
            IsActive    = s.IsActive
        }).ToList();

    private static void PopulateTiers(SponsorFormViewModel vm)
    {
        vm.Tiers = Enum.GetValues<SponsorTier>()
            .OrderByDescending(t => t)
            .Select(t => new SelectListItem
            {
                Value    = ((int)t).ToString(),
                Text     = t.ToString(),
                Selected = t == vm.Tier
            }).ToList();
    }

    private async Task<string> UploadLogoAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        return await fileStorage.UploadAsync(stream, file.FileName, file.ContentType, ct);
    }
}
