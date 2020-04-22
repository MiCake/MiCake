using MiCake.Core.ExceptionHandling;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// Indicates a non critical error message
    /// </summary>
    [Serializable]
    public class SoftMiCakeException : MiCakeException, ISoftMiCakeException
    {
        public SoftMiCakeException(
            string message,
            string details = null,
            string code = null) : base(message, details, code)
        {
        }

        public SoftMiCakeException(
            string message,
            Exception innerException = null,
            string details = null,
            string code = null) : base(message, innerException, code, details)
        {
        }
    }
}
