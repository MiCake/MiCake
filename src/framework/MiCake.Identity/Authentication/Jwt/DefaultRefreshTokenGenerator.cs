using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    internal class DefaultRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public Task<string> Generate(JwtAuthContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
    }
}
