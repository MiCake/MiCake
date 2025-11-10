
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// Default implementation of MiCake builder provider
    /// </summary>
    public sealed class DefaultMiCakeBuilderProvider : IMiCakeBuilderProvider
    {
        private readonly IServiceCollection _services;
        private readonly Type _entryModule;
        private readonly MiCakeApplicationOptions _options;

        public DefaultMiCakeBuilderProvider(
            IServiceCollection services,
            Type entryModule,
            MiCakeApplicationOptions options)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _entryModule = entryModule ?? throw new ArgumentNullException(nameof(entryModule));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IMiCakeBuilder GetMiCakeBuilder()
             => new MiCakeBuilder(_services, _entryModule, _options);
    }
}
