﻿namespace Jifer.Services.Models.Register
{
    using System.ComponentModel.DataAnnotations;
    using static Jifer.Data.Constants.ValidationConstants;

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Полето за първо име е задължително.")]
        [StringLength(
        JUserFirstNameMaxLength,
        MinimumLength = JUserFirstNameMinLength,
        ErrorMessage = "Първото име трябва да бъде между {2} и {1} символа."
        )]
        public string FirstName { get; set; } = null!;

        [StringLength(
            JUserMiddleNameMaxLength,
            MinimumLength = JUserMiddleNameMinLength,
            ErrorMessage = "Второто име трябва да бъде между {2} и {1} символа."
        )]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Полето за фамилия е задължително.")]
        [StringLength(
            JUserLastNameMaxLength,
            MinimumLength = JUserLastNameMinLength,
            ErrorMessage = "Фамилията трябва да бъде между {2} и {1} символа."
        )]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Полето за потребителско име е задължително.")]
        [StringLength(
            JUserUsernameMaxLength,
            MinimumLength = JUserUsernameMinLength,
            ErrorMessage = "Потребителското име трябва да бъде между {2} и {1} символа."
        )]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Полето за имейл е задължително.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        [StringLength(
            EmailsMaxLength,
            MinimumLength = EmailsMinLength,
            ErrorMessage = "Имейлът трябва да бъде между {2} и {1} символа."
        )]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Полето за парола е задължително.")]
        [StringLength(
            PasswordMaxLength,
            MinimumLength =PasswordMinLength,
            ErrorMessage = "Паролата трябва да бъде между {2} и {1} символа."
        )]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Полето за достъпност е задължително.")]
        public Accessibility Accessibility { get; set; }

        public ProfileGender? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string InviteCode { get; set; }

    }
}
