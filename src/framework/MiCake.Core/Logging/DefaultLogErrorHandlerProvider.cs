using MiCake.Core.Abstractions.ExceptionHandling;
using MiCake.Core.Abstractions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Logging
{
    public class DefaultLogErrorHandlerProvider : ILogErrorHandlerProvider
    {
        public DefaultLogErrorHandlerProvider()
        {

        }

        public virtual Action<MiCakeErrorInfo> GetErrorHandler()
        {
            return logErrorHandler;

            void logErrorHandler(MiCakeErrorInfo miCakeErrorInfo)
            {

            }
        }
    }
}
