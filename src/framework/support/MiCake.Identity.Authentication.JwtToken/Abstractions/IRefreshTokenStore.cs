namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// Defined a store can save jwt refresh-token.Used for JWT token to refresh.
    /// </summary>
    public interface IRefreshTokenStore
    {
        /// <summary>
        /// Add refresh-token to store.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>handle key</returns>
        Task<string> StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove refresh-token from store.
        /// </summary>
        /// <param name="refreshTokenHandle">The refresh token handle.</param>
        /// <param name="cancellationToken"></param>
        Task RemoveRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default);

        /// <summary>
        ///Gets the refresh token.
        /// </summary>
        /// <param name="refreshTokenHandle">The refresh token handle.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="RefreshToken"/></returns>
        Task<RefreshToken?> GetRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the refresh token.
        /// </summary>
        /// <param name="refreshTokenHandle"></param>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateRefreshTokenAsync(string refreshTokenHandle, RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh current store data records. This operation will remove all expire refresh-token records.
        /// </summary>
        Task RefreshStoreDataAsync(CancellationToken cancellationToken = default);
    }
}
