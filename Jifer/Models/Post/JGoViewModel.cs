using Jifer.Data.Constants;
using System.ComponentModel.DataAnnotations;

namespace Jifer.Models.Post
{
    public class JGoViewModel
    {
        [Required(ErrorMessage = "Полето за достъпност е задължително.")]
        [StringLength(
        ValidationConstants.JGoTextMaxLength,
        MinimumLength = ValidationConstants.JGoTextMinLength,
        ErrorMessage = "Съдържанието трябва да бъде между {2} и {1} символа."
        )]
        public string Content { get; set; }

        
        [Required(ErrorMessage = "Полето за достъпност е задължително.")]
        public ValidationConstants.Accessibility Visibility { get; set; }
    }
}
