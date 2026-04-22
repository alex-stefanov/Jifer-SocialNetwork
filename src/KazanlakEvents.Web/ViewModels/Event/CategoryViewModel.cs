namespace KazanlakEvents.Web.ViewModels.Event;

public class CategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconCssClass { get; set; }
    public int EventCount { get; set; }
}
