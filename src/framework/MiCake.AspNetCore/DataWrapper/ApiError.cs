using System;

namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// A data warpper exception information
    /// </summary>
    [Serializable]
    public class ApiError : ApiResponse
    {
        /// <summary>
        /// The details of this exception.
        /// </summary>
        public object ErrorDetails { get; set; }

        /// <summary>
        /// The error stack trace info.Only work with debug environment.
        /// </summary>
        public string StackTrace { get; set; }

        public ApiError() { }

        public ApiError(string code, string message)
            : base(code, message)
        {
        }

        public ApiError(string code, string message, object errorDetails)
            : base(code, message)
        {
            ErrorDetails = errorDetails;
        }

        public ApiError(string code, string message, object errorDetails, string stackTrace)
            : base(code, message)
        {
            ErrorDetails = errorDetails;
            StackTrace = stackTrace;
        }
    }
}
