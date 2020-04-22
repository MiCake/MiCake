using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace MiCake.Serilog
{
    public class SerilogProvider
    {
        private IServiceCollection _services;
        public SerilogProvider(IServiceCollection services)
        {
            _services = services;

            var loggerProvides = _services.BuildServiceProvider(false).GetServices<ILoggerFactory>();
            var isAddSerilog = loggerProvides.Any(provide => provide is SerilogLoggerFactory);

            if (!isAddSerilog)
                throw new ArgumentNullException($"{nameof(SerilogLoggerFactory)} is null ," +
                    $"Please check if the serilog has been added. if not,add serilog in you Program.cs ");
        }

        public void AddSerilogInMiCake(Action<SerilogConfigureOption> serilogOptionAction)
        {
            SerilogConfigureOption serilogOption = new SerilogConfigureOption();
            serilogOptionAction(serilogOption);

            //add serilog Log instance if is not has serilog Log 
            //if serilog not configure ,will create a silentLogger.Instance,but its not have HasOverrideMap property.
            var isConfiguraSerilog = Log.Logger.GetType().GetProperty("HasOverrideMap", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public) != null;
            if (!isConfiguraSerilog)
            {
                if (serilogOption.SerilogLoggerConfiguration != null)
                    Log.Logger = serilogOption.SerilogLoggerConfiguration.CreateLogger();
                else
                    Log.Logger = GetDefaultSerilog();
            }
        }

        public Logger GetDefaultSerilog()
        {
            return new LoggerConfiguration()
              .MinimumLevel.Information()
              .WriteTo.Console()
              .WriteTo.File("log.txt",
                  rollingInterval: RollingInterval.Day,
                  rollOnFileSizeLimit: true)
              .CreateLogger();
        }

    }
}
