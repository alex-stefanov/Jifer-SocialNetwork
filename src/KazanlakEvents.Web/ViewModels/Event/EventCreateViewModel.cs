using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KazanlakEvents.Web.ViewModels.Event;

public class EventCreateViewModel
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Short Description")]
    public string? ShortDescription { get; set; }

    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; }

    [Display(Name = "Venue")]
    public Guid? VenueId { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    [Display(Name = "Capacity")]
    public int? Capacity { get; set; }

    [Display(Name = "Wheelchair Accessible")]
    public bool IsAccessible { get; set; }

    [Range(0, 100, ErrorMessage = "Minimum age must be between 0 and 100.")]
    [Display(Name = "Minimum Age")]
    public int? MinAge { get; set; }

    [Display(Name = "Cover Image")]
    public IFormFile? CoverImage { get; set; }

    [Display(Name = "Latitude")]
    public decimal? Latitude { get; set; }

    [Display(Name = "Longitude")]
    public decimal? Longitude { get; set; }

    public List<int> SelectedTagIds { get; set; } = new();

    [MaxLength(100)]
    [Display(Name = "Request new tag")]
    public string? NewTagName { get; set; }

    [MaxLength(200)]
    [Display(Name = "Venue name")]
    public string? NewVenueName { get; set; }

    [MaxLength(300)]
    [Display(Name = "Venue address")]
    public string? NewVenueAddress { get; set; }

    [Display(Name = "Venue latitude")]
    public decimal? NewVenueLat { get; set; }

    [Display(Name = "Venue longitude")]
    public decimal? NewVenueLng { get; set; }

    [Display(Name = "This event has ticketing")]
    public bool HasTicketing { get; set; } = true;

    [Display(Name = "Recurring Event")]
    public bool IsRecurring { get; set; }

    [Display(Name = "Frequency")]
    public string RruleFreq { get; set; } = "WEEKLY";

    [Range(2, 52, ErrorMessage = "Count must be between 2 and 52.")]
    [Display(Name = "Occurrences")]
    public int RruleCount { get; set; } = 4;

    public List<string> RruleByDay { get; set; } = new();

    [Range(1, 31)]
    [Display(Name = "Day of Month")]
    public int RruleByMonthDay { get; set; } = 1;

    public List<TicketTypeInputViewModel> TicketTypeInputs { get; set; } = new();

    public IEnumerable<SelectListItem>? Categories { get; set; }
    public IEnumerable<SelectListItem>? Venues { get; set; }
    public IEnumerable<SelectListItem>? Tags { get; set; }
}
