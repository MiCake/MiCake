using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// Default implementation of <see cref="IApiLoggingConfigProvider"/>
    /// that uses <see cref="IOptionsMonitor{TOptions}"/> to monitor configuration changes from <see cref="MiCakeAspNetOptions"/>.
    /// </summary>
    internal sealed class OptionsApiLoggingConfigProvider : IApiLoggingConfigProvider
    {
        private readonly IOptionsMonitor<MiCakeAspNetOptions> _optionsMonitor;
        private volatile ApiLoggingEffectiveConfig? _cachedConfig;
        private readonly object _lock = new();

        public OptionsApiLoggingConfigProvider(IOptionsMonitor<MiCakeAspNetOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;

            // Subscribe to configuration changes
            _optionsMonitor.OnChange(_ =>
            {
                lock (_lock)
                {
                    _cachedConfig = null;
                }
            });
        }

        public Task<ApiLoggingEffectiveConfig> GetEffectiveConfigAsync(
            CancellationToken cancellationToken = default)
        {
            var cached = _cachedConfig;
            if (cached != null)
            {
                return Task.FromResult(cached);
            }

            lock (_lock)
            {
                _cachedConfig ??= ApiLoggingEffectiveConfig.FromOptions(_optionsMonitor.CurrentValue.ApiLoggingOptions);
                return Task.FromResult(_cachedConfig);
            }
        }

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _cachedConfig = null;
            }
            return Task.CompletedTask;
        }
    }
}
