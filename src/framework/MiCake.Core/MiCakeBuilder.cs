using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// A builder for <see cref="IMiCakeApplication"/>
    /// </summary>
    public class MiCakeBuilder : IMiCakeBuilder
    {
        private readonly MiCakeApplicationOptions _options;
        private readonly Type _entryType;
        private readonly IServiceCollection _services;
        private readonly bool _needNewScope;

        private Action<IMiCakeApplication, IServiceCollection> _configureAction;

        public MiCakeBuilder(
            IServiceCollection services,
            Type entryType,
            MiCakeApplicationOptions options,
            bool needNewScope = false)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _entryType = entryType ?? throw new ArgumentNullException(nameof(entryType));
            _options = options ?? new MiCakeApplicationOptions();
            _needNewScope = needNewScope;

            AddEnvironment();
        }

        public IMiCakeApplication Build()
        {
            // Build the service provider first
            var serviceProvider = _services.BuildServiceProvider();

            // Create the application with the service provider
            var app = new MiCakeApplication(_services, serviceProvider, _options, _needNewScope);

            _configureAction?.Invoke(app, _services);
            app.SetEntry(_entryType);
            
            // Initialize synchronously (blocking) - this is required before the app can be used
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
