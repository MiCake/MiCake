using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.DataWrapper
{
    public class ApiResponse : IResultDataWrapper
    {
        /// <summary>
        /// <see cref="StatusCodes"/>
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Is there any error in this request
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Indication code of business operation error
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Response message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The result data of this request.
        /// </summary>
        public object Result { get; set; }

        public ApiResponse(string message,
                           object result = null,
                           int statusCode = 200)
        {
            StatusCode = statusCode;
            Message = message;
            Result = result;
            IsError = false;
        }

        public ApiResponse(object result, int statusCode = 200)
        {
            StatusCode = statusCode;
            Result = result;
            IsError = false;
        }

        public ApiResponse(string message,
                           string errorCode,
                           object result = null,
                           int statusCode = 200)
        {
            Message = message;
            ErrorCode = errorCode;
            Result = result;
            StatusCode = statusCode;
            IsError = true;
        }

        public ApiResponse(int statusCode)
        {
            StatusCode = statusCode;
            IsError = false;
        }

        public ApiResponse() { }
    }
}
