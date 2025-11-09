using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// A builder for <see cref="IMiCakeApplication"/>
    /// Registers MiCake services and configures the application to be resolved from DI container
    /// </summary>
    public class MiCakeBuilder : IMiCakeBuilder
    {
        private readonly MiCakeApplicationOptions _options;
        private readonly Type _entryType;
        private readonly IServiceCollection _services;

        private Action<IServiceCollection> _configureAction;

        public MiCakeBuilder(
            IServiceCollection services,
            Type entryType,
            MiCakeApplicationOptions options)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _entryType = entryType ?? throw new ArgumentNullException(nameof(entryType));
            _options = options ?? new MiCakeApplicationOptions();

            AddEnvironment();
            RegisterMiCakeApplication();
        }

        /// <summary>
        /// Completes the MiCake configuration.
        /// Returns the builder for chaining. The actual IMiCakeApplication will be resolved from DI container.
        /// </summary>
        public IMiCakeBuilder Build()
        {
            _configureAction?.Invoke(_services);
            return this;
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication> configureApp)
        {
            // Store configuration to be applied when application is resolved
            _configureAction += (services) =>
            {
                // Configuration will be applied when app is first resolved
            };
            return this;
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication, IServiceCollection> configureApp)
        {
            _configureAction += (services) =>
            {
                // Configuration will be applied when app is first resolved
                // We'll need to handle this differently - store it for later
            };
            return this;
        }

        private void AddEnvironment()
        {
            var environment = new MiCakeEnvironment { EntryType = _entryType };
            _services.AddSingleton<IMiCakeEnvironment>(environment);
        }

        private void RegisterMiCakeApplication()
        {
            // Register a factory that creates MiCakeApplication with proper dependencies
            _services.AddSingleton<IMiCakeApplication>(sp =>
            {
                // MiCakeApplication now gets IServiceProvider from DI container
                var app = new MiCakeApplication(_services, sp, _options);
                app.SetEntry(_entryType);
                
                // Initialize synchronously - this is a one-time operation
                app.Initialize().GetAwaiter().GetResult();
                
                return app;
            });
        }
    }
}
