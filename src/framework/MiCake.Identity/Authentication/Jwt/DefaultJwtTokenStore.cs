using MiCake.Core.Util;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    /// <summary>
    /// A default implement for <see cref="IJwtTokenStore"/>.Use in memory cache.
    /// </summary>
    internal class DefaultJwtTokenStore : IJwtTokenStore
    {
        private readonly IDistributedCache _cache;

        public DefaultJwtTokenStore(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task AddRefreshToken(string key, string refreshToken, DateTime expiresTime, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNullOrWhiteSpace(refreshToken, nameof(refreshToken));
            CheckValue.NotNullOrWhiteSpace(key, nameof(key));

            if (expiresTime < DateTime.Now)
                return;

            await _cache.SetAsync(key, EncodingData(new JwtRefreshToken(refreshToken, expiresTime)), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = expiresTime
            }, cancellationToken);
        }

        public async Task<JwtRefreshToken?> GetRefreshToken(string key, CancellationToken cancellationToken = default)
        {
            var result = await _cache.GetAsync(key, cancellationToken);
            if (result == null)
                return null;

            return DecodingData(result);
        }

        public async Task RemoveRefreshToken(string key, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNullOrWhiteSpace(key, nameof(key));

            await _cache.RemoveAsync(key, cancellationToken);
        }

        public async Task RefreshData(CancellationToken cancellationToken = default)
        {
            await _cache.GetAsync("");  // for in memory cache,when get data,will trigger refresh.
        }

        private byte[] EncodingData(object data)
            => JsonSerializer.SerializeToUtf8Bytes(data);

        private JwtRefreshToken DecodingData(byte[] data)
            => JsonSerializer.Deserialize<JwtRefreshToken>(data);
    }
}
