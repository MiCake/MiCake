﻿using MiCake.Core.Util;
using Microsoft.Extensions.Logging;

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
            if (moduleDescriptor.Instance.IsFrameworkLevel && !DebugEnvironment.IsDebug)
                return;

            _logger.LogInformation(preInfo + GetModuleInfoString(moduleDescriptor));
        }

        private string GetModuleInfoString(MiCakeModuleDescriptor moduleDesciptor)
        {
            var moduleType = moduleDesciptor.ModuleType;

            var featerTag = (typeof(IFeatureModule).IsAssignableFrom(moduleType)) ? "[Feature] - " : string.Empty;
            return featerTag + moduleType.Name;
        }
    }
}
