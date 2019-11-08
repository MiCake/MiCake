using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.ExceptionHandling
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
