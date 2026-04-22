using System.Security.Claims;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Infrastructure.Data;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Web.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context) : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        ViewData["HideNavFooter"] = true;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewData["HideNavFooter"] = true;
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, UserRoles.User);

            context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                PreferredLanguage = "bg"
            });
            await context.SaveChangesAsync();

            await signInManager.SignInAsync(user, isPersistent: false);
            await UpdateInitialsClaimAsync(user);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["HideNavFooter"] = true;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["HideNavFooter"] = true;
        if (!ModelState.IsValid) return View(model);

        var result = await signInManager.PasswordSignInAsync(
            model.UserNameOrEmail, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var signedInUser = await userManager.FindByNameAsync(model.UserNameOrEmail);
            if (signedInUser != null) await UpdateInitialsClaimAsync(signedInUser);
            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account is locked. Try again later.");
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.UserNameOrEmail);
        if (user != null)
        {
            result = await signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await UpdateInitialsClaimAsync(user);
                return LocalRedirect(returnUrl ?? "/");
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private async Task UpdateInitialsClaimAsync(ApplicationUser user)
    {
        var profile = await context.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        string initials;
        if (profile?.FirstName?.FirstOrDefault() is char f && profile?.LastName?.FirstOrDefault() is char l)
            initials = $"{char.ToUpper(f)}{char.ToUpper(l)}";
        else
            initials = user.UserName?.Length > 0 ? char.ToUpper(user.UserName[0]).ToString() : "U";

        var existing = (await userManager.GetClaimsAsync(user))
            .FirstOrDefault(c => c.Type == "Initials");

        if (existing != null)
            await userManager.ReplaceClaimAsync(user, existing, new Claim("Initials", initials));
        else
            await userManager.AddClaimAsync(user, new Claim("Initials", initials));

        await signInManager.RefreshSignInAsync(user);
    }

    [HttpGet]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties  = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= "/";

        if (remoteError != null)
        {
            TempData["Error"] = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectToAction(nameof(Login));

        var result = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser != null) await UpdateInitialsClaimAsync(existingUser);
            return LocalRedirect(returnUrl);
        }

        var email     = info.Principal.FindFirstValue(ClaimTypes.Email);
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName  = info.Principal.FindFirstValue(ClaimTypes.Surname)   ?? string.Empty;

        if (email == null)
        {
            TempData["Error"] = "Could not retrieve email from external provider.";
            return RedirectToAction(nameof(Login));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName       = email,
                Email          = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                TempData["Error"] = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Login));
            }

            await userManager.AddToRoleAsync(user, UserRoles.User);

            context.UserProfiles.Add(new UserProfile
            {
                UserId            = user.Id,
                FirstName         = firstName,
                LastName          = lastName,
                PreferredLanguage = "bg"
            });
            await context.SaveChangesAsync();
        }

        await userManager.AddLoginAsync(user, info);
        await signInManager.SignInAsync(user, isPersistent: false);
        await UpdateInitialsClaimAsync(user);
        return LocalRedirect(returnUrl);
    }
}
