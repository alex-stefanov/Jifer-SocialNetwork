namespace Jifer.Services.Models.SendEmail
{
    using System.ComponentModel.DataAnnotations;
    using static Jifer.Data.Constants.ValidationConstants;

    public class SendEmailViewModel
    {
        [Required(ErrorMessage = "Полето за имейл е задължително.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        [StringLength(
            EmailsMaxLength,
            MinimumLength = EmailsMinLength,
            ErrorMessage = "Имейлът трябва да бъде между {2} и {1} символа."
        )]
        public string EmailToBeSent { get; set; } = null!;

        public string? DuplicateEmail { get; set; }
    }
}
