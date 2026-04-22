using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Web.ViewModels.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace KazanlakEvents.Web.Controllers;

[Authorize]
public class NotificationController(
    INotificationService notificationService,
    ICurrentUserService currentUser) : Controller
{
    private const int PageSize = 20;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? filter = null, CancellationToken ct = default)
    {
        var userId = currentUser.UserId!.Value;

        var total  = await notificationService.GetTotalCountAsync(userId, filter, ct);
        var unread = await notificationService.GetUnreadCountAsync(userId, ct);
        var items  = await notificationService.GetUserNotificationsAsync(userId, page, PageSize, filter, ct);

        var vm = new NotificationListViewModel
        {
            CurrentPage   = page,
            TotalPages    = (int)Math.Ceiling(total / (double)PageSize),
            UnreadCount   = unread,
            ActiveFilter  = filter ?? string.Empty,
            Notifications = items.Select(n => new NotificationItemViewModel
            {
                Id        = n.Id,
                Type      = n.Type,
                Title     = n.Title,
                Message   = n.Message,
                LinkUrl   = n.LinkUrl,
                IsRead    = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(Guid id, string? filter = null, CancellationToken ct = default)
    {
        await notificationService.MarkAsReadAsync(id, ct);
        return RedirectToAction(nameof(Index), new { filter });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead(string? filter = null, CancellationToken ct = default)
    {
        await notificationService.MarkAllAsReadAsync(currentUser.UserId!.Value, ct);
        return RedirectToAction(nameof(Index), new { filter });
    }

    [HttpGet]
    public async Task<IActionResult> MarkAsReadAndRedirect(Guid id, string? returnUrl = null, CancellationToken ct = default)
    {
        try { await notificationService.MarkAsReadAsync(id, ct); } catch { /* non-critical */ }
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [OutputCache(Duration = 10)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        var count = await notificationService.GetUnreadCountAsync(currentUser.UserId!.Value, ct);
        return Json(new { count });
    }
}
