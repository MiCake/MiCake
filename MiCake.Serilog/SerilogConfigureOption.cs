using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

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

    }
}
