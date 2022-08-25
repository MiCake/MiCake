using MiCake.Core.Time;
using System.Collections.Concurrent;

namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// A default implement for <see cref="IRefreshTokenStore"/>.Use in memory cache.
    /// </summary>
    internal class DefaultRefreshTokenStore : IRefreshTokenStore
    {
        private static readonly ConcurrentDictionary<string, RefreshToken> _store = new();

        private readonly IAppClock _clock;
        private readonly IRefreshTokenHandleGenerator _handleGenerator;

        public DefaultRefreshTokenStore(IAppClock clock, IRefreshTokenHandleGenerator handleGenerator)
        {
            _clock = clock;
            _handleGenerator = handleGenerator;
        }

        public async Task<string> StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            var key = await _handleGenerator.GenerateHandle(refreshToken, cancellationToken);
            _store.TryAdd(key, refreshToken);

            return key;
        }

        public Task RemoveRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default)
        {
            _store.TryRemove(refreshTokenHandle, out _);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(refreshTokenHandle, out var value);
            return Task.FromResult(value);
        }

        public Task RefreshStoreDataAsync(CancellationToken cancellationToken = default)
        {
            ScanForExpiredItems();
            return Task.CompletedTask;
        }

        private void ScanForExpiredItems()
        {
            foreach (var (key, entry) in _store)
            {
                var refreTokenLifetime = entry.GetExpireDateTime();
                if (refreTokenLifetime > _clock.Now)
                {
                    RemoveRefreshTokenAsync(key);
                }
            }
        }

        public Task UpdateRefreshTokenAsync(string refreshTokenHandle, RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            _store.AddOrUpdate(refreshTokenHandle, refreshToken, (key, value) =>
            {
                return value;
            });

            return Task.CompletedTask;
        }
    }
}
