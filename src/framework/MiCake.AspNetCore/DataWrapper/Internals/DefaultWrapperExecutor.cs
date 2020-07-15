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
        private ConcurrentDictionary<int, CustomModelWithType> _cacheCustomerType;

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

            if (exception is ISoftlyMiCakeException)
            {
                //Given Ok Code for this exception.
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                //Given this exception to context.
                wrapperContext.SoftlyException = exception as SoftlyMiCakeException;

                return WrapSuccesfullysResult(originalData ?? exception.Message, wrapperContext, true);
            }

            var micakeException = exception as MiCakeException;
            ApiError result = new ApiError(exception.Message,
                                           originalData,
                                           micakeException?.Code,
                                           options.IsDebug ? exception.StackTrace : null);

            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual object WrapSuccesfullysResult(object orignalData, DataWrapperContext wrapperContext, bool isSoftException = false)
        {
            CheckValue.NotNull(wrapperContext, nameof(wrapperContext));
            CheckValue.NotNull(wrapperContext.HttpContext, nameof(wrapperContext.HttpContext));
            CheckValue.NotNull(wrapperContext.WrapperOptions, nameof(wrapperContext.WrapperOptions));

            if (orignalData is IResultDataWrapper)
                return orignalData;

            var options = wrapperContext.WrapperOptions;
            var statuCode = (wrapperContext.ResultData as ObjectResult)?.StatusCode ?? wrapperContext.HttpContext.Response.StatusCode;

            if (!options.UseCustomModel)
            {
                if (isSoftException)
                {
                    var softlyException = wrapperContext.SoftlyException;

                    return new ApiResponse(softlyException.Message)
                    {
                        Result = softlyException.Details,
                        ErrorCode = softlyException.Code,
                        IsError = true,
                    };
                }

                if (orignalData is ProblemDetails problemDetails)
                {
                    return options.WrapProblemDetails ? new ApiResponse(problemDetails.Title)
                    {
                        Result = problemDetails.Detail,
                        StatusCode = statuCode,
                        IsError = true,
                    }
                    : orignalData;
                }

                return new ApiResponse(ResponseMessage.Success, orignalData) { StatusCode = statuCode };
            }
            else
            {
                if (options.CustomModelConfig == null || options.CustomModelConfig.Count == 0)
                    return orignalData;

                //create customer model.
                var customerModel = CreateCustomModel(wrapperContext);
                if (customerModel == null)
                    return orignalData;

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

        private object AssignValueToCustomType(CustomModelWithType customModelInfo, DataWrapperContext wrapperContext)
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

        private Type CreateCustomEmitType(CustomWrapperModel customWrapperModel)
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

        class CustomModelWithType
        {
            public CustomWrapperModel ModelConfig { get; set; }
            public Type EmitType { get; set; }

            public CustomModelWithType(CustomWrapperModel customModel, Type emitType)
            {
                ModelConfig = customModel;
                EmitType = emitType;
            }
        }
    }
}
