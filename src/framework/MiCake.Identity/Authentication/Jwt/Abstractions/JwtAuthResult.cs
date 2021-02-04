namespace MiCake.Identity.Authentication.Jwt
{
    /// <summary>
    /// A result for <see cref="IJwtAuthManager"/> create or refresh.
    /// Include token and refresh-token.
    /// </summary>
    public class JwtAuthResult
    {
        /// <summary>
        /// The AccessToken for JWT
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The RefreshToken for JWT
        /// </summary>
        public string RefreshToken { get; set; }
    }
}
