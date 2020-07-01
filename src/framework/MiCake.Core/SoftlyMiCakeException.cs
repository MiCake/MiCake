using System;

namespace MiCake.Core
{
    /// <summary>
    /// Indicates a non critical error message
    /// </summary>
    [Serializable]
    public class SoftlyMiCakeException : MiCakeException, ISoftlyMiCakeException
    {
        public SoftlyMiCakeException() : base()
        {
        }

        public SoftlyMiCakeException(
            string message,
            string details = null,
            string code = null) : base(message, details, code)
        {
        }

        public SoftlyMiCakeException(
            string message,
            Exception innerException,
            string details = null,
            string code = null) : base(message, innerException, code, details)
        {
        }
    }
}
