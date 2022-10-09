namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// Use to generate a store key for jwt refresh-token.
    /// <para>
    ///     When use <see cref="IRefreshTokenStore"/> to store or find a refresh-token,must give a unique key.
    ///     Use this interface can customize your strategy.
    /// </para>
    /// </summary>
    public interface IRefreshTokenHandleGenerator
    {
        /// <summary>
        /// Generate a handle key for store refresh-token.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GenerateHandle(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    }
}
