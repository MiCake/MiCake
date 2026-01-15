using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Implementation of module configuration context.
    /// Stores module registration information for later initialization.
    /// </summary>
    internal class MiCakeModuleConfigurationContext : IMiCakeModuleConfigurationContext
    {
        private readonly List<ModuleRegistration> _moduleRegistrations = [];

        public MiCakeApplicationOptions Options { get; }

        internal IReadOnlyList<ModuleRegistration> ModuleRegistrations => _moduleRegistrations;

        public MiCakeModuleConfigurationContext(
            IServiceCollection services,
            MiCakeApplicationOptions options)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void AddModule<TModule>(Action<object>? configureModule = null) where TModule : MiCakeModule
        {
            AddModule(typeof(TModule), configureModule);
        }

        public void AddModule(Type moduleType, Action<object>? configureModule = null)
        {
            ArgumentNullException.ThrowIfNull(moduleType);

            MiCakeModuleHelper.CheckModule(moduleType);

            _moduleRegistrations.Add(new ModuleRegistration
            {
                ModuleType = moduleType,
                ConfigureAction = configureModule
            });
        }

        internal class ModuleRegistration
        {
            public required Type ModuleType { get; set; }
            public Action<object>? ConfigureAction { get; set; }
        }
    }
}
