using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// The options for config micake jwt authentication.
    /// It's use to create token.Please keep consistent with the configuration of the verification token
    /// </summary>
    public class MiCakeJwtOptions
    {
        /// <summary>
        /// This key is used for signing credentials
        /// </summary>
        public byte[] SecurityKey { get; set; } = Encoding.Default.GetBytes("micake-256-bit-secret");

        /// <summary>
        /// Get or set the signature algorithm.
        /// You can get Common algorithms with <see cref="SecurityAlgorithms"/>
        /// 
        /// <para>
        ///     If this parameter is not specified, <see cref="SecurityAlgorithms.HmacSha256"/> will be used
        /// </para>
        /// </summary>
        public string Algorithm { get; set; } = SecurityAlgorithms.HmacSha256;

        /// <summary>
        /// The "iss" (issuer) claim identifies the principal that issued the JWT.
        /// </summary>
        public string Issuer { get; set; } = "MiCake Application";

        /// <summary>
        /// The "aud" (audience) claim identifies the recipients that the JWT is intended for.
        /// 
        /// Default value is : "MiCake Client"
        /// </summary>
        public string Audience { get; set; } = "MiCake Client";

        /// <summary>
        /// The "exp" (expiration time) claim identifies the expiration time on or after which the JWT MUST NOT be accepted for processing.
        /// <para>
        ///     Lifetime of access token in seconds (defaults to 3600 seconds / 1 hour)
        /// </para>
        /// </summary>
        public uint AccessTokenLifetime { get; set; } = 3600;

        /// <summary>
        /// Gets or sets the <see cref="EncryptingCredentials"/> used to create a encrypted security token.
        /// This value can be null.
        /// </summary>
        public EncryptingCredentials? EncryptingCredentials { get; set; }

        /// <summary>
        /// Whether the refresh token scheme is needed
        /// </summary>
        public bool UseRefreshToken { get; set; } = true;

        /// <summary>
        /// <see cref="RefreshTokenUsageMode"/>
        /// </summary>
        public RefreshTokenUsageMode RefreshTokenMode { get; set; } = RefreshTokenUsageMode.Reuse;

        /// <summary>
        /// Maximum lifetime of a refresh token in seconds. Defaults to 2592000 seconds / 30 days.
        /// If value is less than 0,It's mean unlimited.
        /// </summary>
        public uint RefreshTokenLifetime { get; set; } = 2592000;

        /// <summary>
        /// Always delete old refresh token record when exchange refresh token.
        /// Default value is true.
        /// </summary>
        public bool DeleteWhenExchangeRefreshToken { get; set; } = true;
    }
}
