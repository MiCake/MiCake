using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

// Expose namespace withou abstractons.
namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// A simple supporter for Jwt Token.It's include create token/refresh-token, refresh token , delete token etc. 
    /// <para>
    ///     When you don't use other authentication schemes, such as (OAuth or Oidc), this will be your choice.
    /// </para>
    /// </summary>
    public interface IJwtAuthManager
    {
        /// <summary>
        /// Create a jwt token with <see cref="MiCakeJwtOptions"/> config.
        /// </summary>
        /// <param name="miCakeUser"><see cref="IMiCakeUser{TKey}"/>,If user property has <see cref="JwtClaimAttribute"/>,will inculde it.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="JwtTokenAuthResult"/></returns>
        Task<JwtTokenAuthResult> CreateToken(IMiCakeUser miCakeUser, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a jwt token with <see cref="MiCakeJwtOptions"/> config.
        /// </summary>
        /// <param name="miCakeUser"><see cref="IMiCakeUser{TKey}"/>,If user property has <see cref="JwtClaimAttribute"/>,will inculde it.</param>
        /// <param name="subjectId">the token subject</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="JwtTokenAuthResult"/></returns>
        Task<JwtTokenAuthResult> CreateToken(IMiCakeUser miCakeUser, string subjectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a jwt token with claims and <see cref="MiCakeJwtOptions"/> config.
        /// </summary>
        /// <param name="claims">A collection of (key,value) pairs representing System.Security.Claims.Claim for this token.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="JwtTokenAuthResult"/></returns>
        Task<JwtTokenAuthResult> CreateToken(Claim[] claims, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a jwt token with claims and <see cref="MiCakeJwtOptions"/> config.
        /// </summary>
        /// <param name="claims">A collection of (key,value) pairs representing System.Security.Claims.Claim for this token.</param>
        /// <param name="subjectId">the token subject</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="JwtTokenAuthResult"/></returns>
        Task<JwtTokenAuthResult> CreateToken(Claim[] claims, string subjectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh curren token.
        /// </summary>
        /// <param name="refreshTokenHandle">the handle of refresh token</param>
        /// <param name="accessToken">old access token.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>a new <see cref="JwtTokenAuthResult"/></returns>
        Task<JwtTokenAuthResult> Refresh(string refreshTokenHandle, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decode Jwt token with <see cref="MiCakeJwtOptions"/>.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(ClaimsPrincipal, JwtSecurityToken)> DecodeJwtToken(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revoke current refresh token.
        /// </summary>
        /// <param name="refreshTokenHandle"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> RevokeRefreshToken(string refreshTokenHandle, CancellationToken cancellationToken = default);
    }
}
