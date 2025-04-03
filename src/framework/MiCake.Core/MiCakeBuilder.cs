using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// A build for <see cref="IMiCakeApplication"/>
    /// </summary>
    public class MiCakeBuilder : IMiCakeBuilder
    {
        private readonly MiCakeApplicationOptions _options;
        private readonly Type _entryType;
        private readonly IServiceCollection _services;
        private readonly bool _needNewScopeed;

        private Action<IMiCakeApplication, IServiceCollection> _configureAction;

        public MiCakeBuilder(IServiceCollection services,
                             Type entryType,
                             MiCakeApplicationOptions options,
                             bool needNewScope = false)
        {
            _services = services;
            _entryType = entryType;
            _options = options ?? new MiCakeApplicationOptions();

            _needNewScopeed = needNewScope;

            AddEnvironment();
        }

        public IMiCakeApplication Build()
        {
            var app = new MiCakeApplication(_services, _options, _needNewScopeed);

            _configureAction?.Invoke(app, _services);
            app.SetEntry(_entryType);
            app.Initialize().GetAwaiter().GetResult();

            return app;
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication> configureApp)
        {
            return ConfigureApplication((app, s) => configureApp(app));
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication, IServiceCollection> configureApp)
        {
            _configureAction += configureApp;
            return this;
        }

        private void AddEnvironment()
        {
            var environment = new MiCakeEnvironment { EntryType = _entryType };

            _services.AddSingleton<IMiCakeEnvironment>(environment);
        }
    }
}
