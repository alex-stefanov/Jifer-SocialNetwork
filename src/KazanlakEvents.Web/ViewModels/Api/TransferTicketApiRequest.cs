using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class TransferTicketApiRequest
{
    [Required, EmailAddress]
    public string RecipientEmail { get; set; } = string.Empty;
}
