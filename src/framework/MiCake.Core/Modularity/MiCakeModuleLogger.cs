using MiCake.Util.Diagnostics.Environment;
using Microsoft.Extensions.Logging;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Logger for MiCake module system.
    /// </summary>
    internal class MiCakeModuleLogger(ILogger logger)
    {
        public ILogger _logger = logger;

        public void LogModuleInfo(MiCakeModuleDescriptor moduleDescriptor, string preInfo = "")
        {
            if (moduleDescriptor.Instance.IsFrameworkLevel && !DebugEnvironment.IsDebug)
                return;

            _logger.LogInformation(preInfo + GetModuleInfoString(moduleDescriptor));
        }

        private static string GetModuleInfoString(MiCakeModuleDescriptor moduleDesciptor)
        {
            return moduleDesciptor.ModuleType?.Name;
        }
    }
}
