using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    /// <summary>
    /// Use to generate a store key for jwt refresh-token.
    /// <para>
    ///     When use <see cref="IJwtTokenStore"/> to store or find a refresh-token,must give a unique key.
    ///     Use this interface can customize your strategy.
    /// </para>
    /// </summary>
    public interface IJwtStoreKeyGenerator
    {
        /// <summary>
        /// Generate a key for store refresh-token.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GenerateKey(JwtAuthContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve a key according to the context.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> RetrieveKey(JwtAuthContext context, CancellationToken cancellationToken = default);
    }
}
