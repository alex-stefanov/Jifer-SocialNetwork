namespace Jifer.Services.Models.Login
{
    using System.ComponentModel.DataAnnotations;

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Моля, въведете потребителско име.")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Моля, въведете парола.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }
}
