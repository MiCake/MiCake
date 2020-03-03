using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core.Builder
{
    /// <summary>
    /// a build for <see cref="IMiCakeApplication"/>
    /// </summary>
    public class MiCakeAppBuilder : IMiCakeAppBuilder
    {
        private MiCakeApplicationOptions _options;
        private Type _entryType;
        private IServiceCollection _services;
        private bool _needNewScopeed;

        private Action<IMiCakeApplication, IServiceCollection> _configureAction;

        public MiCakeAppBuilder(
            [NotNull] IServiceCollection services,
            [NotNull] Type entryType,
            MiCakeApplicationOptions options,
            bool needNewScope = false
            )
        {
            _services = services;
            _entryType = entryType;
            _options = options ?? new MiCakeApplicationOptions();

            _needNewScopeed = needNewScope;
        }

        public IMiCakeApplication Build()
        {
            var app = new MiCakeApplication(_services, _options, null);

            _configureAction?.Invoke(app, _services);
            app.SetEntry(_entryType);
            app.Initialize();

            return app;
        }

        public IMiCakeAppBuilder ConfigureApplication(Action<IMiCakeApplication> configureApp)
        {
            return ConfigureApplication((app, s) => configureApp(app));
        }

        public IMiCakeAppBuilder ConfigureApplication(Action<IMiCakeApplication, IServiceCollection> configureApp)
        {
            _configureAction += configureApp;
            return this;
        }
    }
}
