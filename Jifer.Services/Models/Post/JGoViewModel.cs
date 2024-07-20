namespace Jifer.Services.Models.Post
{
    using System.ComponentModel.DataAnnotations;
    using static Jifer.Data.Constants.ValidationConstants;

    public class JGoViewModel
    {
        [Required(ErrorMessage = "Полето за достъпност е задължително.")]
        [StringLength(
        JGoTextMaxLength,
        MinimumLength = JGoTextMinLength,
        ErrorMessage = "Съдържанието трябва да бъде между {2} и {1} символа."
        )]
        public string Content { get; set; } = null!;
        
        [Required(ErrorMessage = "Полето за достъпност е задължително.")]
        public Accessibility Visibility { get; set; }
    }
}
