namespace Jifer.Data.Constants
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Holds all enum constants
    /// </summary>
    [Comment("ValidationConstants Class")]
    public static class ValidationConstants
    {
        /// <summary>
        /// Valid genders for the user.
        /// </summary>
        public enum ProfileGender
        {
            [Comment("Gender equal to male")]
            Male,

            [Comment("Gender equal to female")]
            Female,

            [Comment("Any other gender which is not male nor female")]
            Other
        }

        /// <summary>
        /// Valid profile accessibilities.
        /// </summary>
        public enum Accessibility
        {
            [Comment("Visible only to friends")]
            FriendsOnly,

            [Comment("Visible to friends of friends only")]
            FriendsOfFriendsOnly,

            [Comment("Visible to the public")]
            Public
        }

        /// <summary>
        /// Valid statuses for a friendship.
        /// </summary>
        public enum FriendshipStatus
        {
            [Comment("Status that awaits response")]
            Pending,

            [Comment("Status that marks a positive response")]
            Confirmed,

            [Comment("Status that marks a negatuve response")]
            Rejected,

            [Comment("Status that marks a response which is withdraw")]
            Withdrawn
        }

        /// <summary>
        /// Max length of a User's first name.
        /// </summary>
        public const int JUserFirstNameMaxLength = 64;

        /// <summary>
        /// Max length of a User's middle name.
        /// </summary>
        public const int JUserMiddleNameMaxLength = 50;

        /// <summary>
        /// Max length of a User's last name.
        /// </summary>
        public const int JUserLastNameMaxLength = 64;

        /// <summary>
        /// Max length of a User's username.
        /// </summary>
        public const int JUserUsernameMaxLength = 64;
        
        /// <summary>
        /// Max length of emails.
        /// </summary>
        public const int EmailsMaxLength = 100;

        /// <summary>
        /// Max length of passwords.
        /// </summary>
        public const int PasswordMaxLength = 120;

        /// <summary>
        /// Max length of the content in a JGo.
        /// </summary>
        public const int JGoTextMaxLength = 150;

        /// <summary>
        /// Min length of a User's first name.
        /// </summary>
        public const int JUserFirstNameMinLength = 3;

        /// <summary>
        /// Min length of a User's middle name.
        /// </summary>
        public const int JUserMiddleNameMinLength = 3;

        /// <summary>
        /// Min length of a User's last name.
        /// </summary>
        public const int JUserLastNameMinLength = 3;

        /// <summary>
        /// Min length of a User's username.
        /// </summary>
        public const int JUserUsernameMinLength = 4;

        /// <summary>
        /// Min length of emails.
        /// </summary>
        public const int EmailsMinLength = 4;

        /// <summary>
        /// Min length of passwords.
        /// </summary>
        public const int PasswordMinLength = 6;

        /// <summary>
        /// Min length of the content in a JGo.
        /// </summary>
        public const int JGoTextMinLength = 10;
    }
}
