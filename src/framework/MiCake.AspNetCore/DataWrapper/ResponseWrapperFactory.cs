namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// Factory delegate for creating success response wrappers.
    /// </summary>
    /// <param name="context">Context information about the request and response</param>
    /// <returns>The wrapped response object</returns>
    public delegate object SuccessWrapperFactory(WrapperContext context);

    /// <summary>
    /// Factory delegate for creating error response wrappers.
    /// </summary>
    /// <param name="context">Context information about the error and request</param>
    /// <returns>The wrapped error response object</returns>
    public delegate object ErrorWrapperFactory(ErrorWrapperContext context);

    /// <summary>
    /// Provides factory methods for creating response wrappers.
    /// Allows full customization of response format while maintaining simplicity.
    /// </summary>
    public class ResponseWrapperFactory
    {
        /// <summary>
        /// Factory for wrapping successful responses.
        /// </summary>
        public SuccessWrapperFactory SuccessFactory { get; set; }

        /// <summary>
        /// Factory for wrapping error responses.
        /// </summary>
        public ErrorWrapperFactory ErrorFactory { get; set; }

        /// <summary>
        /// Creates a default factory using StandardResponse and ErrorResponse.
        /// </summary>
        public static ResponseWrapperFactory CreateDefault(DataWrapperOptions options)
        {
            return new ResponseWrapperFactory
            {
                SuccessFactory = context => 
                {
                    // Check if this is a SlightException being wrapped as a success response
                    if (context.OriginalData is Internals.SlightExceptionData slightData)
                    {
                        var code = string.IsNullOrWhiteSpace(slightData.Code)
                            ? options.DefaultCodeSetting.Success
                            : slightData.Code;
                        
                        return new ApiResponse(
                            code: code,
                            message: slightData.Message,
                            data: slightData.Details
                        );
                    }
                    
                    return new ApiResponse(
                        code: options.DefaultCodeSetting.Success,
                        message: null,
                        data: context.OriginalData
                    );
                },
                ErrorFactory = context => new ErrorResponse(
                    code: options.DefaultCodeSetting.Error,
                    message: context.Exception?.Message ?? "An error occurred",
                    details: null,
                    stackTrace: options.ShowStackTraceWhenError ? context.Exception?.StackTrace : null
                )
            };
        }
    }
}
