using System;

namespace MiCake.AspNetCore.DataWrapper
{
    [Serializable]
    public class ApiResponse : IResultDataWrapper
    {
        /// <summary>
        /// Indication code of business operation.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Response message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The result data of this request.
        /// </summary>
        public object Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public ApiResponse(string code, string message, object data)
        {
            Data = data;
            Code = code;
            Message = message;
        }
    }
}
