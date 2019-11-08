using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace MiCake.Serilog
{
    public class SerilogProvider
    {
        public void GetDefaultSerilog()
        {
            new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();
        }
    }
}
