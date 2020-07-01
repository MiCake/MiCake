using System;

namespace MiCake.Core
{
    /// <summary>
    /// Base exception type for Micake farmework.
    /// </summary>
    [Serializable]
    public class MiCakeException : Exception
    {
        /// <summary>
        /// Indicates a code to represent the exception.
        /// </summary>
        public virtual string Code { get; set; }

        /// <summary>
        /// Some details about the error
        /// </summary>
        public virtual object Details { get; set; }

        protected MiCakeException()
        {
        }

        public MiCakeException(
            string message,
            string details = null,
            string code = null) : base(message)
        {
            Code = code;
            Details = details;
        }

        public MiCakeException(
            string message,
            Exception innerException,
            string details = null,
            string code = null) : base(message, innerException)
        {
            Code = code;
            Details = details;
        }
    }
}
