using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using static MiCake.Core.MiCakeApplicationOptions;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Logger for MiCake module system.
    /// Handles welcome brand display, dependency graph visualization, and lifecycle logging.
    /// </summary>
    internal class MiCakeModuleLogger(ILogger logger)
    {
        private readonly ILogger _logger = logger;
        private bool _welcomeBrandPrinted;

        private const string _welcomeBrandString =
@"
    __  __     ____         __  ____ ______      __
   / / / /__  / / /___     /  |/  (_) ____/___ _/ /_____
  / /_/ / _ \/ / / __ \   / /|_/ / / /   / __ `/ //_/ _ \
 / __  /  __/ / / /_/ /  / /  / / / /___/ /_/ / ,< /  __/
/_/ /_/\___/_/_/\____/  /_/  /_/_/\____/\__,_/_/|_|\___/
";

        /// <summary>
        /// Logs the welcome brand and dependency graph if this is the first call.
        /// </summary>
        /// <param name="modules">The collection of modules</param>
        /// <param name="resolver">The module dependency resolver</param>
        /// <param name="options">Application options for configuration</param>
        public void LogWelcomeAndDependencyGraph(
            IMiCakeModuleCollection modules,
            ModuleDependencyResolver resolver,
            MiCakeApplicationOptions options)
        {
            if (_welcomeBrandPrinted)
                return;

            _welcomeBrandPrinted = true;

            var printing = options.Printing ?? new PrintingOptions();

            // Print welcome brand if enabled
            if (printing.WelcomeBrand)
            {
                _logger.LogInformation(_welcomeBrandString);
            }

            // Print dependency graph if enabled and there are modules
            if (printing.DependencyGraph && modules.Count > 0)
            {
                var dependencyGraph = resolver.GetDependencyGraph();
                if (!string.IsNullOrEmpty(dependencyGraph))
                {
                    _logger.LogInformation("Module Dependency Graph: {0}", dependencyGraph);
                }
            }
        }

        /// <summary>
        /// Logs module lifecycle information for a group of modules in one line.
        /// Format: [LifecycleName]: ModuleA -> ModuleB -> ModuleC
        /// </summary>
        /// <param name="modules">The modules to log</param>
        /// <param name="lifecycleName">The name of the lifecycle phase</param>
        public void LogModuleLifecycle(IEnumerable<MiCakeModuleDescriptor> modules, string lifecycleName)
        {
            var filteredModules = modules.Where(m => !m.Instance.IsFrameworkLevel)
                                         .ToList();

            if (filteredModules.Count == 0)
                return;

            var moduleChain = string.Join(" -> ", filteredModules.Select(m => m.ModuleType?.Name ?? "Unknown"));
            _logger.LogInformation("[{0}]: {1}", lifecycleName, moduleChain);
        }

        /// <summary>
        /// Logs a single module's information (legacy method for backward compatibility).
        /// </summary>
        /// <param name="moduleDescriptor">The module to log</param>
        /// <param name="preInfo">Prefix information</param>
        public void LogModuleInfo(MiCakeModuleDescriptor moduleDescriptor, string preInfo = "")
        {
            if (moduleDescriptor.Instance.IsFrameworkLevel)
                return;

            _logger.LogInformation(preInfo + GetModuleInfoString(moduleDescriptor));
        }

        private static string GetModuleInfoString(MiCakeModuleDescriptor moduleDesciptor)
        {
            return moduleDesciptor.ModuleType?.Name ?? "Unknown";
        }
    }
}
