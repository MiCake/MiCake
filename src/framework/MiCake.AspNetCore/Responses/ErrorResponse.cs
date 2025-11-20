namespace MiCake.AspNetCore.Responses
{
    /// <summary>
    /// Standard response wrapper model for error responses.
    /// </summary>
    public class ErrorResponse : IResponseWrapper
    {
        /// <summary>
        /// Error code identifying the type of error.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Error message describing what went wrong.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Additional error details (validation errors, inner exception details, etc.).
        /// </summary>
        public object? Details { get; set; }

        /// <summary>
        /// Stack trace information. Only included when enabled in configuration.
        /// </summary>
        public string? StackTrace { get; set; }

        public ErrorResponse()
        {
        }

        public ErrorResponse(string? code, string? message)
        {
            Code = code;
            Message = message;
        }

        public ErrorResponse(string? code, string? message, object? details, string? stackTrace = null)
        {
            Code = code;
            Message = message;
            Details = details;
            StackTrace = stackTrace;
        }
    }
}
