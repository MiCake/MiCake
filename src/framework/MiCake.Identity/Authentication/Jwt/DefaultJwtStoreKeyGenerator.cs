using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    internal class DefaultJwtStoreKeyGenerator : IJwtStoreKeyGenerator
    {
        public Task<string> GenerateKey(JwtAuthContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(context.RefreshToken);

        public Task<string> RetrieveKey(JwtAuthContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(context.RefreshToken);
    }
}
