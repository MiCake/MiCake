using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    public sealed class DefaultMiCakeBuilderProvider : IMiCakeBuilderProvider
    {
        private IServiceCollection _services;
        private Type _entryModule;
        private MiCakeApplicationOptions _options;
        private bool _needScope;

        public DefaultMiCakeBuilderProvider(
            [NotNull]IServiceCollection services,
            [NotNull]Type entryModule,
            [NotNull]MiCakeApplicationOptions options,
            bool needScope)
        {
            _services = services;
            _entryModule = entryModule;
            _options = options;
            _needScope = needScope;
        }

        public IMiCakeBuilder GetMiCakeBuilder()
             => new MiCakeBuilder(_services, _entryModule, _options, _needScope);
    }
}
