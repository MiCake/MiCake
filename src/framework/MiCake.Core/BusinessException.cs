using System;

namespace MiCake.Core
{
    /// <summary>
    /// Indicates an exception that can be safely shown to end users.
    /// </summary>
    public class BusinessException : Exception, IBusinessException
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string? Code { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public object? Details { get; }

        public BusinessException() : base()
        {
        }

        public BusinessException(
            string message,
            string? details = null,
            string? code = null) : base(message)
        {
            Details = details;
            Code = code;
        }

        public BusinessException(
            string message,
            Exception innerException,
            string? details = null,
            string? code = null) : base(message, innerException)
        {
            Details = details;
            Code = code;
        }
    }
}
