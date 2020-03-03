using MiCake.Core.ExceptionHandling;
using System;

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
