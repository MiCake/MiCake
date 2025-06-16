using MiCake.Core;
using MiCake.Core.Util;
using MiCake.Core.Util.CommonTypes;
using MiCake.Core.Util.Reflection;
using MiCake.Core.Util.Reflection.Emit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Default implementation for <see cref="IDataWrapperExecutor"/>
    /// </summary>
    internal class DefaultWrapperExecutor : IDataWrapperExecutor
    {
        // the cache of custom model.
        private readonly ConcurrentDictionary<int, CustomModelWithType> _cacheCustomerType;

        public DefaultWrapperExecutor()
        {
            _cacheCustomerType = new ConcurrentDictionary<int, CustomModelWithType>();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual object WrapFailedResult(object originalData, Exception exception, DataWrapperContext wrapperContext)
        {
            var httpContext = wrapperContext.HttpContext;
            var options = wrapperContext.WrapperOptions;

            CheckValue.NotNull(httpContext, nameof(HttpContext));
            CheckValue.NotNull(options, nameof(DataWrapperOptions));

            if (exception is ISlightException)
            {
                //Given Ok Code for this exception.
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                //Given this exception to context.
                wrapperContext.SoftlyException = exception as SlightMiCakeException;

                return WrapSuccesfullysResult(originalData ?? exception.Message, wrapperContext, true);
            }

            var micakeException = exception as MiCakeException;
            ApiError result = new(GetApiResponseCode(micakeException?.Code, WrapperResultType.Error, wrapperContext.WrapperOptions), exception.Message, micakeException?.Details, options.ShowStackTraceWhenError ? exception.StackTrace : null);

            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual object WrapSuccesfullysResult(object originalData, DataWrapperContext wrapperContext, bool isSoftException = false)
        {
            CheckValue.NotNull(wrapperContext, nameof(wrapperContext));
            CheckValue.NotNull(wrapperContext.HttpContext, nameof(wrapperContext.HttpContext));
            CheckValue.NotNull(wrapperContext.WrapperOptions, nameof(wrapperContext.WrapperOptions));

            if (originalData is IResultDataWrapper)
                return originalData;

            var options = wrapperContext.WrapperOptions;
            if (!options.UseCustomModel)
            {
                if (isSoftException)
                {
                    var softlyException = wrapperContext.SoftlyException;

                    return new ApiResponse(GetApiResponseCode(softlyException.Code, WrapperResultType.Success, wrapperContext.WrapperOptions), softlyException.Message, softlyException.Details);
                }

                if (originalData is ProblemDetails problemDetails)
                {
                    return options.WrapProblemDetails ? new ApiResponse(GetApiResponseCode(null, WrapperResultType.ProblemDetails, wrapperContext.WrapperOptions), problemDetails.Title, originalData) : originalData;
                }

                return new ApiResponse(GetApiResponseCode(null, WrapperResultType.Success, wrapperContext.WrapperOptions), null, originalData);
            }
            else
            {
                if (options.CustomModelConfig == null || options.CustomModelConfig.Count == 0)
                    return originalData;

                //create customer model.
                var customerModel = CreateCustomModel(wrapperContext);
                if (customerModel == null)
                    return originalData;

                return customerModel;
            }
        }

        /// <summary>
        /// Create customModel instance,and given value by config.
        /// </summary>
        /// <param name="wrapperContext"><see cref="DataWrapperContext"/></param>
        /// <returns>customModel instance.</returns>
        protected virtual object CreateCustomModel(DataWrapperContext wrapperContext)
        {
            var currentHttpCode = wrapperContext.HttpContext.Response.StatusCode;

            if (_cacheCustomerType.TryGetValue(currentHttpCode, out var resultConfigInfo))
            {
                return AssignValueToCustomType(resultConfigInfo, wrapperContext);
            }

            var customModels = wrapperContext.WrapperOptions.CustomModelConfig;

            CustomWrapperModel optimizationCustomModel = null;
            foreach (var customModel in customModels)
            {
                if (customModel.Key.IsInRange(currentHttpCode))
                {
                    optimizationCustomModel = customModel.Value;
                    break;
                }
            }

            if (optimizationCustomModel == null)
            {
                //If it is not empty, it means that there is no configuration,
                //then when the status code is encountered in the future, the source data will be returned
                _cacheCustomerType.TryAdd(currentHttpCode, null);
                return null;
            }

            var emitType = CreateCustomEmitType(optimizationCustomModel);
            var customModelInfo = new CustomModelWithType(optimizationCustomModel, emitType);
            _cacheCustomerType.TryAdd(currentHttpCode, customModelInfo);

            return AssignValueToCustomType(customModelInfo, wrapperContext);
        }

        private static object AssignValueToCustomType(CustomModelWithType customModelInfo, DataWrapperContext wrapperContext)
        {
            if (customModelInfo == null)
                return null;

            var modelInstance = Activator.CreateInstance(customModelInfo.EmitType);
            foreach (var customerProperty in customModelInfo.ModelConfig.GetAllConfigProperties())
            {
                var propertyValue = customerProperty.Value(wrapperContext);
                customModelInfo.EmitType.GetProperty(customerProperty.Key)?.SetValue(modelInstance, propertyValue);
            }
            return modelInstance;
        }

        private static Type CreateCustomEmitType(CustomWrapperModel customWrapperModel)
        {
            var dyClass = EmitHelper.CreateClass(customWrapperModel.ModelName,
                                     MiCakeReflectionPredefined.DynamicAssembly,
                                     MiCakeReflectionPredefined.DynamicModule,
                                     baseType: typeof(BaseResultDataWrapper));

            foreach (var customerProperty in customWrapperModel.GetAllConfigProperties())
            {
                CheckValue.NotNullOrEmpty(customerProperty.Key, nameof(customerProperty.Key));
                EmitHelper.CreateProperty(dyClass, customerProperty.Key, typeof(object));
            }

            return dyClass.CreateType();
        }

        public static string GetApiResponseCode(string? businessCode, WrapperResultType successStation, DataWrapperOptions wrapperOptions)
        {
            var defaultCodeSetting = wrapperOptions.DefaultCodeSetting;

            if (string.IsNullOrWhiteSpace(businessCode))
            {
                businessCode = successStation switch
                {
                    WrapperResultType.Success => defaultCodeSetting.Success,
                    WrapperResultType.Error => defaultCodeSetting.Error,
                    WrapperResultType.ProblemDetails => defaultCodeSetting.ProblemDetails,
                    _ => throw new ArgumentOutOfRangeException(nameof(successStation), "Unknown result type for data wrapper.")
                };
            }

            return businessCode;
        }

        public class CustomModelWithType(CustomWrapperModel customModel, Type emitType)
        {
            public CustomWrapperModel ModelConfig { get; set; } = customModel;
            public Type EmitType { get; set; } = emitType;
        }

        public enum WrapperResultType
        {
            Success,
            Error,
            ProblemDetails
        }
    }
}
