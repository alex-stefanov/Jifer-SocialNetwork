using Jifer.Data.Constants;
using System.ComponentModel.DataAnnotations;

namespace Jifer.Models.SendEmail
{
    public class SendEmailViewModel
    {
        [Required(ErrorMessage = "Полето за имейл е задължително.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        [StringLength(
            ValidationConstants.EmailsMaxLength,
            MinimumLength = ValidationConstants.EmailsMinLength,
            ErrorMessage = "Имейлът трябва да бъде между {2} и {1} символа."
        )]
        public string EmailToBeSent { get; set; } = null!;
    }
}
