using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Linq;
using Serilog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiCake.Serilog.ExceptionHanding;
using MiCake.Core.Abstractions.Logging;
using ILogger = Serilog.ILogger;
using Serilog.Core;
using System.Reflection;

namespace MiCake.Serilog
{
    public class SerilogProvider
    {
        private IServiceCollection _services;
        public SerilogProvider(IServiceCollection services)
        {
            _services = services;

            var loggerProvides = _services.BuildServiceProvider().GetServices<ILoggerFactory>();
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

            //replace errorHandler
            var errorHandlerInstance = serilogOption.logErrorHandlerProvider ?? new SerilogErrorHandlerProvider(serilogOption);
            _services.Replace(new ServiceDescriptor(typeof(ILogErrorHandlerProvider), errorHandlerInstance));
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
