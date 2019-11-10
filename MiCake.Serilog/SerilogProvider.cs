using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Linq;
using Serilog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiCake.Serilog.ExceptionHanding;

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

            //add serilog Log instance


            //replace errorHandler
            var errorHandlerInstance = serilogOption.logErrorHandlerProvider ?? new SerilogErrorHandlerProvider(serilogOption);
            _services.Replace(new ServiceDescriptor(ILogErrorHandlerProvider, errorHandlerInstance, ServiceLifetime.Singleton));
        }

        public void GetDefaultSerilog()
        {
            new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
        }

    }
}
