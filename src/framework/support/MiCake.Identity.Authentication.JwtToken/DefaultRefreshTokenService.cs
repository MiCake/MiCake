using MiCake.Core.Time;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace MiCake.Identity.Authentication.JwtToken
{
    internal class DefaultRefreshTokenService : IRefreshTokenService
    {
        private readonly IAppClock _clock;
        private readonly MiCakeJwtOptions _options;
        private readonly IRefreshTokenStore _tokenStore;

        public DefaultRefreshTokenService(IAppClock appClock, IOptions<MiCakeJwtOptions> options, IRefreshTokenStore tokenStore)
        {
            _clock = appClock;
            _options = options.Value;
            _tokenStore = tokenStore;
        }

        public virtual async Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, string subjectId, CancellationToken cancellationToken = default)
        {
            var refreshToken = new RefreshToken(subjectId, subject)
            {
                CreationTime = _clock.Now,
                Lifetime = (int)_options.RefreshTokenLifetime,
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
                if (_options.DeleteWhenExchangeRefreshToken)
                {
                    await _tokenStore.RemoveRefreshTokenAsync(handle, cancellationToken);
                }
                else
                {
                    refreshToken.ConsumedTime = _clock.Now;
                    await _tokenStore.UpdateRefreshTokenAsync(handle, refreshToken, cancellationToken);
                }

                needsCreate = true;
            }
            else if (_options.RefreshTokenMode == RefreshTokenUsageMode.Reuse)
            {
                refreshToken.Version++;
                refreshToken.CreationTime = _clock.Now;
                refreshToken.Lifetime = (int)_options.RefreshTokenLifetime;

                needsUpdate = true;
            }

            if (needsCreate)
            {
                // set it to null so that we save non-consumed token
                var newToken = CloenRefreshToken(refreshToken);
                newToken.CreationTime = _clock.Now;

                handle = await _tokenStore.StoreRefreshTokenAsync(newToken, cancellationToken);
            }
            else if (needsUpdate)
            {
                await _tokenStore.UpdateRefreshTokenAsync(handle, refreshToken, cancellationToken);
            }

            return handle;
        }

        private RefreshToken CloenRefreshToken(RefreshToken refreshToken)
        {
            return new RefreshToken(refreshToken.SubjectId, refreshToken.Subject)
            {
                Lifetime = refreshToken.Lifetime,
            };
        }
    }
}
