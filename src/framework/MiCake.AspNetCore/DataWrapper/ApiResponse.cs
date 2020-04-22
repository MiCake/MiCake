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

        public ApiResponse(int statusCode)
        {
            StatusCode = statusCode;
            IsError = false;
        }

        public ApiResponse() { }
    }
}
