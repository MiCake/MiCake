using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    /// <summary>
    /// Defined a store can save jwt refresh-token.Used for JWT token to refresh.
    /// </summary>
    public interface IJwtTokenStore
    {
        /// <summary>
        /// Add refresh-token to store,set an expires time at the same time.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="refreshToken"></param>
        /// <param name="expiresTime"></param>
        /// <param name="cancellationToken"></param>
        Task AddRefreshToken(string key, string refreshToken, DateTime expiresTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove refresh-token from store.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        Task RemoveRefreshToken(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get refresh-token info.Include expires time.
        /// If has no reulst,return null.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="JwtRefreshToken"/></returns>
        Task<JwtRefreshToken?> GetRefreshToken(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh store data,It's mean remove all expired refresh-token.
        /// </summary>
        Task RefreshData(CancellationToken cancellationToken = default);
    }
}
