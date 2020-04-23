namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// A data warpper exception information
    /// </summary>
    public class ApiError : IResultDataWrapper
    {
        /// <summary>
        /// Is there any error in this request
        /// </summary>
        public bool IsError { get; set; } = true;

        /// <summary>
        /// The message of this exception.
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// The details of this exception.
        /// </summary>
        public object Details { get; set; }

        /// <summary>
        /// Indication code of business operation error
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// The error stack trace info.Only work with debug environment.
        /// </summary>
        public string StackTrace { get; set; }

        public ApiError(string message,
                        object details = null,
                        string errorCode = null,
                        string stackTrace = null,
                        bool isError = true)
        {
            ExceptionMessage = message;
            Details = details;
            ErrorCode = errorCode;
            StackTrace = stackTrace;
            IsError = isError;
        }
    }
}
