using MiCake.Identity.Authentication.JwtToken.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.JwtToken
{
    public class DefaultRefreshTokenHandleGenerator : IRefreshTokenHandleGenerator
    {
        public virtual Task<string> GenerateHandle(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
