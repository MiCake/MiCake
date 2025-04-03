using System;

namespace MiCake.Core
{
    /// <summary>
    /// Indicates a non critical error message
    /// </summary>
    [Serializable]
    public class SlightMiCakeException : MiCakeException, ISlightException
    {
        public SlightMiCakeException() : base()
        {
        }

        public SlightMiCakeException(
            string message,
            string? details = null,
            string? code = null) : base(message, details, code)
        {
        }

        public SlightMiCakeException(
            string message,
            Exception innerException,
            string? details = null,
            string? code = null) : base(message, innerException, code, details)
        {
        }
    }
}
