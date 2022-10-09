namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// A result for <see cref="IJwtAuthManager"/> create or refresh.
    /// Include token and  refresh-token(if open use refresh-token).
    /// </summary>
    public class JwtTokenAuthResult
    {
        /// <summary>
        /// The AccessToken for JWT
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// The RefreshToken for JWT
        /// </summary>
        public string? RefreshToken { get; set; }
    }
}
