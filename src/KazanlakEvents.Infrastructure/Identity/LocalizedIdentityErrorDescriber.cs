using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace KazanlakEvents.Infrastructure.Identity;

public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer _localizer;

    public LocalizedIdentityErrorDescriber(IStringLocalizerFactory factory)
    {
        // Use factory to avoid circular dependency (SharedResource marker lives in Web layer)
        _localizer = factory.Create("KazanlakEvents.Web.Resources.SharedResource", "KazanlakEvents.Web");
    }

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = string.Format(_localizer["IdentityErrorDuplicateUserName"].Value, userName)
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = string.Format(_localizer["IdentityErrorDuplicateEmail"].Value, email)
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = _localizer["IdentityErrorInvalidEmail"].Value
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = string.Format(_localizer["IdentityErrorPasswordTooShort"].Value, length)
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = _localizer["IdentityErrorPasswordRequiresDigit"].Value
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = _localizer["IdentityErrorPasswordRequiresLower"].Value
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = _localizer["IdentityErrorPasswordRequiresUpper"].Value
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = _localizer["IdentityErrorPasswordRequiresNonAlphanumeric"].Value
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = string.Format(_localizer["IdentityErrorPasswordRequiresUniqueChars"].Value, uniqueChars)
    };

    public override IdentityError UserAlreadyHasPassword() => new()
    {
        Code = nameof(UserAlreadyHasPassword),
        Description = _localizer["IdentityErrorUserAlreadyHasPassword"].Value
    };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = nameof(UserLockoutNotEnabled),
        Description = _localizer["IdentityErrorUserLockoutNotEnabled"].Value
    };

    public override IdentityError UserNotInRole(string role) => new()
    {
        Code = nameof(UserNotInRole),
        Description = string.Format(_localizer["IdentityErrorUserNotInRole"].Value, role)
    };

    public override IdentityError UserAlreadyInRole(string role) => new()
    {
        Code = nameof(UserAlreadyInRole),
        Description = string.Format(_localizer["IdentityErrorUserAlreadyInRole"].Value, role)
    };

    public override IdentityError InvalidToken() => new()
    {
        Code = nameof(InvalidToken),
        Description = _localizer["IdentityErrorInvalidToken"].Value
    };

    public override IdentityError LoginAlreadyAssociated() => new()
    {
        Code = nameof(LoginAlreadyAssociated),
        Description = _localizer["IdentityErrorLoginAlreadyAssociated"].Value
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = string.Format(_localizer["IdentityErrorInvalidUserName"].Value, userName)
    };

    public override IdentityError InvalidRoleName(string? role) => new()
    {
        Code = nameof(InvalidRoleName),
        Description = string.Format(_localizer["IdentityErrorInvalidRoleName"].Value, role)
    };

    public override IdentityError DuplicateRoleName(string role) => new()
    {
        Code = nameof(DuplicateRoleName),
        Description = string.Format(_localizer["IdentityErrorDuplicateRoleName"].Value, role)
    };

    public override IdentityError ConcurrencyFailure() => new()
    {
        Code = nameof(ConcurrencyFailure),
        Description = _localizer["IdentityErrorConcurrencyFailure"].Value
    };
}
