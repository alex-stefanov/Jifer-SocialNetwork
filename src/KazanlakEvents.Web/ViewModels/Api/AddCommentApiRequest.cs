using System.ComponentModel.DataAnnotations;

namespace KazanlakEvents.Web.ViewModels.Api;

public class AddCommentApiRequest
{
    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
