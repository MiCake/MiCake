using MiCake.AspNetCore;
using MiCake.AspNetCore.DataWrapper;
using Microsoft.AspNetCore.Mvc;

namespace BaseMiCakeApplication.MiCakeFeatures
{
    public static class DataWrappedFeature
    {
        /// <summary>
        /// Example 1: Use default StandardResponse wrapper (simplest approach)
        /// </summary>
        public static MiCakeAspNetOptions UseDefaultWrapper(this MiCakeAspNetOptions options)
        {
            // No configuration needed - StandardResponse is used by default
            // Response format: { "code": "0", "message": null, "data": <your-data> }
            return options;
        }

        /// <summary>
        /// Example 2: Customize default response codes
        /// </summary>
        public static MiCakeAspNetOptions UseCustomCodes(this MiCakeAspNetOptions options)
        {
            options.DataWrapperOptions.DefaultCodeSetting.Success = "SUCCESS";
            options.DataWrapperOptions.DefaultCodeSetting.Error = "ERROR";
            return options;
        }

        /// <summary>
        /// Example 3: Use completely custom response wrapper
        /// This is the new simplified way to customize response format
        /// </summary>
        public static MiCakeAspNetOptions UseCustomWrapper(this MiCakeAspNetOptions options)
        {
            options.DataWrapperOptions.WrapperFactory = new ResponseWrapperFactory
            {
                // Custom success response
                SuccessFactory = context => new
                {
                    company = "MiCake",
                    statusCode = context.StatusCode,
                    success = true,
                    result = context.OriginalData,
                    timestamp = System.DateTime.UtcNow
                },

                // Custom error response
                ErrorFactory = context => new
                {
                    company = "MiCake",
                    statusCode = context.StatusCode,
                    success = false,
                    error = new
                    {
                        message = context.Exception?.Message ?? "Unknown error",
                        details = context.Exception is MiCake.Core.MiCakeException micakeEx 
                            ? micakeEx.Details 
                            : null
                    },
                    timestamp = System.DateTime.UtcNow
                }
            };

            return options;
        }

        /// <summary>
        /// Example 4: Use strongly-typed custom wrapper model
        /// </summary>
        public static MiCakeAspNetOptions UseStronglyTypedWrapper(this MiCakeAspNetOptions options)
        {
            options.DataWrapperOptions.WrapperFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new CustomApiResponse
                {
                    Code = "OK",
                    Payload = context.OriginalData,
                    ServerTime = System.DateTime.UtcNow
                },

                ErrorFactory = context => new CustomErrorResponse
                {
                    ErrorCode = "ERROR",
                    ErrorMessage = context.Exception?.Message,
                    ServerTime = System.DateTime.UtcNow
                }
            };

            return options;
        }

        /// <summary>
        /// Example 5: Configure FluentValidation error handling
        /// </summary>
        public static MiCakeAspNetOptions ConfigureValidationErrors(this MiCakeAspNetOptions options)
        {
            // Keep ASP.NET Core's ValidationProblemDetails format (recommended for FluentValidation)
            options.DataWrapperOptions.WrapValidationProblemDetails = false;
            
            // Or wrap them in your custom format
            // options.DataWrapperOptions.WrapValidationProblemDetails = true;

            return options;
        }

        #region Legacy Example (Obsolete)

        /// <summary>
        /// Legacy example using CustomWrapperModel (obsolete - kept for backward compatibility)
        /// </summary>
        [System.Obsolete("Use UseCustomWrapper() instead for simpler configuration")]
        public static MiCakeAspNetOptions UseCustomModel(this MiCakeAspNetOptions options)
        {
            // Old complex way - use ResponseWrapperFactory instead
            options.DataWrapperOptions.WrapperFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new
                {
                    company = "MiCake",
                    statusCode = context.StatusCode,
                    result = context.OriginalData
                }
            };

            return options;
        }

        #endregion
    }

    // Example custom response models
    public class CustomApiResponse : IResponseWrapper
    {
        public string Code { get; set; }
        public object Payload { get; set; }
        public System.DateTime ServerTime { get; set; }
    }

    public class CustomErrorResponse : IResponseWrapper
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public System.DateTime ServerTime { get; set; }
    }
}
