using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.JwtToken
{
    internal class DefaultRefreshTokenHandleGenerator : IRefreshTokenHandleGenerator
    {
        public virtual Task<string> GenerateHandle(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
