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
    }
}
