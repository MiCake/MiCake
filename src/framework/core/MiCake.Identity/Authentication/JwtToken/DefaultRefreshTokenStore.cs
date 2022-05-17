using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// A default implement for <see cref="IRefreshTokenStore"/>.Use in memory cache.
    /// </summary>
    internal class DefaultRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, RefreshToken> _store = new();

        private readonly IRefreshTokenHandleGenerator _handleGenerator;

        public DefaultRefreshTokenStore(IRefreshTokenHandleGenerator handleGenerator)
        {
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
            ScanForExpiredItems(this);
            return Task.CompletedTask;
        }

        private static void ScanForExpiredItems(DefaultRefreshTokenStore store)
        {
            var now = DateTimeOffset.Now; ;
            foreach (var (key, entry) in store._store)
            {
                var refreTokenLifetime = entry.CreationTime.AddSeconds(entry.Lifetime);
                if (refreTokenLifetime > now)
                {
                    store.RemoveRefreshTokenAsync(key);
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
