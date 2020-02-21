using Microsoft.Extensions.Logging;

namespace MiCake.Core.Logging
{
    /// <summary>
    /// Realize the logging level for the service
    /// which is convenient to be processed by the log handler
    /// </summary>
    public interface IMarkLogLevel
    {
        LogLevel Level { get; set; }
    }
}
