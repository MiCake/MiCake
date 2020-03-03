using MiCake.Core.ExceptionHandling;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Serialization;

namespace MiCake.Core
{
    [Serializable]
    public class SoftMiCakeException : MiCakeException, ISoftMiCakeException
    {
        public override LogLevel Level => LogLevel.Warning;

        public SoftMiCakeException()
        {
        }

        public SoftMiCakeException(string message) : base(message)
        {
        }

        public SoftMiCakeException(string message,
            string code = null,
            string details = null,
            Exception innerException = null) : base(message, code, details, innerException, LogLevel.Warning)
        {
        }

        protected SoftMiCakeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
