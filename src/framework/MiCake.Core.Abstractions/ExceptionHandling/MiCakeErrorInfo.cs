using System;

namespace MiCake.Core.ExceptionHandling
{
    public struct MiCakeErrorInfo
    {
        public string Code { get; }

        public string Details { get; }

        public Exception ExceptionInstance { get; }

        public MiCakeErrorInfo(string code, string details, Exception exception)
        {
            Code = code;
            Details = details;
            ExceptionInstance = exception;
        }
    }
}
