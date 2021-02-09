using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
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
        /// <returns><see cref="JwtAuthResult"/></returns>
        Task<JwtAuthResult> CreateToken(IMiCakeUser miCakeUser, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a jwt token with claims and <see cref="MiCakeJwtOptions"/> config.
        /// </summary>
        /// <param name="claims">A collection of (key,value) pairs representing System.Security.Claims.Claim for this token.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="JwtAuthResult"/></returns>
        Task<JwtAuthResult> CreateToken(Claim[] claims, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh curren token by use refresh-token.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="accessToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>a new <see cref="JwtAuthResult"/></returns>
        Task<JwtAuthResult> Refresh(string refreshToken, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decode Jwt token with <see cref="MiCakeJwtOptions"/>.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(ClaimsPrincipal, JwtSecurityToken)> DecodeJwtToken(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove current refresh token.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        Task RemoveRefreshToken(string refreshToken, CancellationToken cancellationToken = default);
    }
}
