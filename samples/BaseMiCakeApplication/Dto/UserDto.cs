namespace BaseMiCakeApplication.Dto
{
    /// <summary>
    /// DTO for representing user information in responses.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the user's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user's avatar URL.
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// Gets or sets the user's age.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number.
        /// </summary>
        public string Phone { get; set; }
    }
}
