namespace KazanlakEvents.Web.ViewModels.Event;

public class SeriesViewModel
{
    public Guid SeriesId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string RecurrenceRule { get; set; } = string.Empty;
    public IReadOnlyList<EventCardViewModel> Events { get; set; } = [];
}
