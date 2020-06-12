using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MiCake.Identity.Authentication
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
        ///     If this parameter is not specified, <see cref="SecurityAlgorithms.HmacSha256Signature"/> will be used
        /// </para>
        /// </summary>
        public string Algorithm { get; set; } = SecurityAlgorithms.HmacSha256Signature;

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
        ///     Default value is one day(1440 min): 24 * 60.
        /// </para>
        /// </summary>
        public int ExpirationMinutes { get; set; } = 1440;

        /// <summary>
        /// Gets or sets the <see cref="EncryptingCredentials"/> used to create a encrypted security token.
        /// This value can be null.
        /// </summary>
        public EncryptingCredentials EncryptingCredentials { get; set; }

        public void FromOtherOptions(MiCakeJwtOptions otherOptios)
        {
            SecurityKey = otherOptios.SecurityKey;
            Algorithm = otherOptios.Algorithm;
            Issuer = otherOptios.Issuer;
            Audience = otherOptios.Audience;
            ExpirationMinutes = otherOptios.ExpirationMinutes;
            EncryptingCredentials = otherOptios.EncryptingCredentials;
        }
    }
}
