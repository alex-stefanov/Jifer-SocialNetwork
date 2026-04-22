namespace KazanlakEvents.Domain.Enums;

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Moderator = "Moderator";
    public const string BlogAuthor = "BlogAuthor";
    public const string User = "User";

    public static readonly string[] All =
    [
        SuperAdmin, Admin, Organizer, Moderator, BlogAuthor, User
    ];
}
