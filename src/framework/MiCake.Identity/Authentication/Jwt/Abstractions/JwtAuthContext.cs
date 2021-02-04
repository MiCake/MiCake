using System.Security.Claims;

namespace MiCake.Identity.Authentication.Jwt
{
    /// <summary>
    /// The context for MiCake Jwt auth.
    /// </summary>
    public class JwtAuthContext
    {
        /// <summary>
        /// The access token of current issue process.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The refresh-token of current issue process.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// The clamis of  current issue process.
        /// </summary>
        public ClaimsIdentity Claims { get; set; }

        /// <summary>
        /// <see cref="MiCakeJwtOptions"/>
        /// </summary>
        public MiCakeJwtOptions JwtOptions { get; set; }

        /// <summary>
        /// Indicate create a new token or refresh a token.
        /// </summary>
        public JwtAuthContextState State { get; set; }
    }

    public enum JwtAuthContextState
    {
        CreateNew = 0,
        Refresh = 1
    }
}
