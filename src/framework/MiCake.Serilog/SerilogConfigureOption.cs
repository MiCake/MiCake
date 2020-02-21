using MiCake.Core.Logging;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MiCake.Serilog
{
    public class SerilogConfigureOption
    {
        public bool AutoLogError { get; set; }

        /// <summary>
        /// Errors higher than this level will be recorded automatically.
        /// this option is work on <see cref="AutoLogError"/> is true.
        /// </summary>
        public LogLevel FilterLogLevel { get; set; }

        /// <summary>
        /// provide a customer error log handler to log you error info with serilog.
        /// <see cref="ILogErrorHandlerProvider "/>
        /// </summary>
        public ILogErrorHandlerProvider logErrorHandlerProvider { get; set; }

        /// <summary>
        /// <see cref="LoggerConfiguration"/>
        /// </summary>
        public LoggerConfiguration SerilogLoggerConfiguration { get; set; }

    }
}
