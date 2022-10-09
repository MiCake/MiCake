using System.Security.Claims;

namespace MiCake.Identity.Authentication.JwtToken
{
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Creates the refresh token.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="subjectId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, string subjectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the refresh token.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, CancellationToken cancellationToken = default);
    }
}
