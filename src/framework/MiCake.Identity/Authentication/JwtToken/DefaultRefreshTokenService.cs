using MiCake.Identity.Authentication.JwtToken.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.JwtToken
{
    public class DefaultRefreshTokenService : IRefreshTokenService
    {
        private readonly MiCakeJwtOptions _options;
        private readonly IRefreshTokenStore _tokenStore;

        public DefaultRefreshTokenService(IOptions<MiCakeJwtOptions> options, IRefreshTokenStore tokenStore)
        {
            _options = options.Value;
            _tokenStore = tokenStore;
        }

        public virtual async Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, string subjectId, CancellationToken cancellationToken = default)
        {
            int lifetime;
            if (_options.RefreshTokenMode == RefreshTokenUsageMode.Reuse)
            {
                lifetime = _options.SlidingRefreshTokenLifetime;
            }
            else
            {
                lifetime = _options.AbsoluteRefreshTokenLifetime;
            }

            var refreshToken = new RefreshToken(subjectId, subject)
            {
                CreationTime = DateTimeOffset.Now,
                Lifetime = lifetime,
            };

            var handle = await _tokenStore.StoreRefreshTokenAsync(refreshToken, cancellationToken);
            return handle;
        }

        public virtual async Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            bool needsCreate = false;
            bool needsUpdate = false;

            if (_options.RefreshTokenMode == RefreshTokenUsageMode.Recreate)
            {
                refreshToken.ConsumedTime = DateTimeOffset.UtcNow.DateTime;
                await _tokenStore.UpdateRefreshTokenAsync(handle, refreshToken);

                needsCreate = true;
            }
            else if (_options.RefreshTokenMode == RefreshTokenUsageMode.Reuse)
            {
                var currentLifetime = (int)(refreshToken.CreationTime - DateTimeOffset.UtcNow.UtcDateTime).TotalSeconds;
                var newLifetime = currentLifetime + _options.SlidingRefreshTokenLifetime;

                if (_options.AbsoluteRefreshTokenLifetime > 0 && newLifetime > _options.AbsoluteRefreshTokenLifetime)
                {
                    newLifetime = _options.AbsoluteRefreshTokenLifetime;
                }

                refreshToken.Lifetime = newLifetime;
                needsUpdate = true;
            }
            else if (_options.RefreshTokenMode == RefreshTokenUsageMode.RecreateBeforeOverdue)
            {
                var expireTime = refreshToken.CreationTime.AddSeconds(refreshToken.Lifetime);
                var beforeOverdueTime = expireTime.AddMinutes((_options.RecreateRefreshTokenBeforeOverdueMinutes) * -1);
                var now = DateTimeOffset.UtcNow.DateTime;

                needsCreate = expireTime >= now && now >= beforeOverdueTime;
            }

            if (needsCreate)
            {
                // set it to null so that we save non-consumed token
                refreshToken.ConsumedTime = null;
                handle = await _tokenStore.StoreRefreshTokenAsync(refreshToken);
            }
            else if (needsUpdate)
            {
                await _tokenStore.UpdateRefreshTokenAsync(handle, refreshToken);
            }

            return handle;
        }
    }
}
