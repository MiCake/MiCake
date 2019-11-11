using MiCake.Core;
using MiCake.Core.Abstractions.ExceptionHandling;
using MiCake.Core.Abstractions.Logging;
using MiCake.Core.ExceptionHandling;
using System;

namespace MiCake.Serilog.ExceptionHanding
{
    /// <summary>
    /// log error info with serilog
    /// </summary>
    public class SerilogErrorHandlerProvider : ILogErrorHandlerProvider
    {
        private SerilogConfigureOption _serilogConfigure;
        public SerilogErrorHandlerProvider(SerilogConfigureOption serilogConfigure)
        {
            _serilogConfigure = serilogConfigure;
        }

        public Action<MiCakeErrorInfo> GetErrorHandler()
        {
            return serilogErrorHandler;

            void serilogErrorHandler(MiCakeErrorInfo miCakeError)
            {

            }
        }
    }
}
