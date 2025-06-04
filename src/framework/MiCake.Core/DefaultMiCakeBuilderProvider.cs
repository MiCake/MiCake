
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    public sealed class DefaultMiCakeBuilderProvider(
        IServiceCollection services,
        Type entryModule,
        MiCakeApplicationOptions options,
        bool needScope) : IMiCakeBuilderProvider
    {
        private readonly IServiceCollection _services = services;
        private readonly Type _entryModule = entryModule;
        private readonly MiCakeApplicationOptions _options = options;
        private readonly bool _needScope = needScope;

        public IMiCakeBuilder GetMiCakeBuilder()
             => new MiCakeBuilder(_services, _entryModule, _options, _needScope);
    }
}
