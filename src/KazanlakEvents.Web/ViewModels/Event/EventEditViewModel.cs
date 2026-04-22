using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Event;

public class EventEditViewModel : EventCreateViewModel
{
    public Guid Id { get; set; }
    public string? ExistingCoverImageUrl { get; set; }
    public EventStatus Status { get; set; }
    public IReadOnlyList<EventImageViewModel> ExistingImages { get; set; } = new List<EventImageViewModel>();
}
