using Jifer.Data.Constants;
using System.ComponentModel.DataAnnotations;

namespace Jifer.Models.SendEmail
{
    public class SendEmailViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(ValidationConstants.EmailsMaxLength, MinimumLength = 4)]
        public string EmailToBeSent { get; set; } = null!;
    }
}
