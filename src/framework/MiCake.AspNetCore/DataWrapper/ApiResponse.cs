namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// Standard response wrapper model for successful responses.
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class ApiResponse<T> : IResponseWrapper where T : notnull
    {
        /// <summary>
        /// Business operation status code.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Response message describing the result.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The actual response data.
        /// </summary>
        public T Data { get; set; } = default!;

        public ApiResponse()
        {
        }

        public ApiResponse(string? code, string? message, T? data)
        {
            Code = code;
            Message = message;
            Data = data ?? default!;
        }
    }

    /// <summary>
    /// Non-generic version of standard response for backward compatibility.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        public ApiResponse()
        {
        }

        public ApiResponse(string? code, string? message, object? data)
            : base(code, message, data)
        {
        }
    }
}
