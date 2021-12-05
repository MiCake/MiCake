using Microsoft.AspNetCore.Http;
using System;

namespace MiCake.AspNetCore.DataWrapper
{
    [Serializable]
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

        public ApiResponse() { }

        public ApiResponse(string message, int statusCode = 200)
        {
            StatusCode = statusCode;
            Message = message;
            IsError = false;
        }

        public ApiResponse(string message, object result)
        {
            Result = result;
            StatusCode = 200;
            Message = message;
            IsError = false;
        }
    }
}
