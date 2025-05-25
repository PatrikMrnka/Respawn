namespace RespawnApi.Domain.Enums
{
    /// <summary>
    /// Represents the different user roles within the application.
    /// </summary>
    public class UserRoles
    {
        /// <summary>
        /// The role for users with administrative privileges, allowing them to manage the application and its users.
        /// </summary>
        public const string Administrator = "Administrátor";

        /// <summary>
        /// The role for users with moderator privileges, allowing them to oversee user interactions and content.
        /// </summary>
        public const string Spravce = "Správce";

        /// <summary>
        /// The role for users with limited privileges, typically for general users who can interact with the application but do not have administrative or moderator capabilities.
        /// </summary>
        public const string Uzivatel = "Hráč";
    }
}
