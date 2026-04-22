using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class LogHoursApiRequest
{
    [Required, Range(0.01, 24.0)]
    public decimal Hours { get; set; }
}
