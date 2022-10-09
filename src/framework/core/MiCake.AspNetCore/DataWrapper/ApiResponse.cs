namespace MiCake.AspNetCore.DataWrapper
{
    [Serializable]
    public class ApiResponse : IWrappedResponse
    {
        public string? Code { get; set; }

        /// <summary>
        /// Is there any error in this request
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Response message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The result data of this request.
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Response success data.
        /// </summary>
        public static ApiResponse Success(object result, string? code = null)
        {
            return new ApiResponse
            {
                Result = result,
                Code = code,
                HasError = false
            };
        }

        /// <summary>
        /// Response error data.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static ApiResponse Failure(string message, string? code = null)
        {
            return new ApiResponse
            {
                Message = message,
                Code = code,
                HasError = true
            };
        }
    }

    [Serializable]
    public class ApiResponse<TData> : ApiResponse where TData : notnull
    {
        public new TData? Result { get; set; }

        public static ApiResponse<TData> Success(TData result, string? code = null)
        {
            return new ApiResponse<TData>
            {
                Result = result,
                Code = code,
                HasError = false
            };
        }

        public new static ApiResponse<TData> Failure(string message, string? code = null)
        {
            return new ApiResponse<TData>
            {
                Message = message,
                Code = code,
                HasError = true
            };
        }
    }
}
