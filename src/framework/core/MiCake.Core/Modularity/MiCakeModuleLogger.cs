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

        public void LogModuleInfo(string phaseStr, IMiCakeModuleCollection phaseModules)
        {
            var moduleStr = string.Join("->", phaseModules.ToList().Where(s => !s.IsCoreModule));

            _logger.LogInformation($"MiCake Phase - {phaseStr} : {moduleStr}");
        }
    }
}
