using KazanlakEvents.Domain.Enums;

namespace KazanlakEvents.Web.ViewModels.Sponsor;

public class SponsorIndexViewModel
{
    public IReadOnlyList<SponsorViewModel> Gold      { get; set; } = new List<SponsorViewModel>();
    public IReadOnlyList<SponsorViewModel> Silver    { get; set; } = new List<SponsorViewModel>();
    public IReadOnlyList<SponsorViewModel> Bronze    { get; set; } = new List<SponsorViewModel>();
    public IReadOnlyList<SponsorViewModel> Community { get; set; } = new List<SponsorViewModel>();
}
