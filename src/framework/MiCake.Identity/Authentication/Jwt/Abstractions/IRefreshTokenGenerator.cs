using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    /// <summary>
    /// Defined a generator for create refresh-token.
    /// </summary>
    public interface IRefreshTokenGenerator
    {
        /// <summary>
        /// Generate a refresh-token by <see cref="JwtAuthContext"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> Generate(JwtAuthContext context, CancellationToken cancellationToken = default);
    }
}
