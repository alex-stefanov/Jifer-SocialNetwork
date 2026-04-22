using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Admin;

public class UserWarningViewModel
{
    public Guid Id { get; set; }
    public string? IssuedByName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public WarningType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
