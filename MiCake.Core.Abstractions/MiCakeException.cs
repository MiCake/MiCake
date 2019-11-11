using MiCake.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MiCake.Core.Abstractions
{
    /// <summary>
    /// Base exception type for Micake farmework.
    /// </summary>
    [Serializable]
    public class MiCakeException : Exception, IMarkLogLevel
    {
        public virtual string Code { get; set; }

        public virtual string Details { get; set; }

        public virtual LogLevel Level { get; set; }

        public MiCakeException()
        {
        }

        public MiCakeException(string message) : base(message)
        {
        }

        public MiCakeException(string message,
            string code = null,
            string details = null,
            Exception innerException = null,
            LogLevel logLevel = LogLevel.Error) : base(message, innerException)
        {
            Code = code;
            Details = details;
            Level = logLevel;
        }

        protected MiCakeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
