namespace BaseMiCakeApplication.Dto
{
    /// <summary>
    /// DTO for login response results.
    /// </summary>
    public class LoginResultDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether the user was found.
        /// </summary>
        public bool HasUser { get; set; }

        /// <summary>
        /// Gets or sets the JWT access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the user information.
        /// </summary>
        public UserDto UserInfo { get; set; }

        /// <summary>
        /// Factory method to create a "no user" response.
        /// </summary>
        /// <returns>A LoginResultDto indicating no user was found</returns>
        public static LoginResultDto NoUser() => new() { HasUser = false };
    }
}
