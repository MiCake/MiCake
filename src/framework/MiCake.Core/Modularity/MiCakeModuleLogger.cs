using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// use to log module lifetime info
    /// </summary>
    internal class MiCakeModuleLogger
    {
        public ILogger _logger;

        public MiCakeModuleLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogModuleInfo(MiCakeModuleDescriptor moduleDescriptor, string preInfo = "")
        {
            if (moduleDescriptor.ModuleInstance.IsFrameworkLevel && !DebugEnvironment.IsDebug)
                return;

            _logger.LogInformation(preInfo + GetModuleInfoString(moduleDescriptor));
        }

        private string GetModuleInfoString(MiCakeModuleDescriptor moduleDesciptor)
        {
            var moduleType = moduleDesciptor.Type;

            var featerTag = (typeof(IFeatureModule).IsAssignableFrom(moduleType)) ? "[Feature] - " : string.Empty;
            return featerTag + moduleType.Name;
        }
    }
}
