using System;

namespace MiCake.Identity.Authentication.Jwt
{
    /// <summary>
    /// A sturct for Jwt refresh-token
    /// </summary>
    public struct JwtRefreshToken
    {
        /// <summary>
        /// A value of refresh-token
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// refresh-token expires time
        /// </summary>
        public DateTime ExpiresTime { get; set; }

        public JwtRefreshToken(string refreshToken, DateTime expiresTime)
        {
            RefreshToken = refreshToken;
            ExpiresTime = expiresTime;
        }
    }
}
