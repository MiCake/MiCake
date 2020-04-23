using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using MiCake.Core.Util.Reflection.Emit;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MiCake.AspNetCore.Tests")]
namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Default implementation for <see cref="IDataWrapperExecutor"/>
    /// </summary>
    internal class DefaultWrapperExecutor : IDataWrapperExecutor
    {
        private Type _cacheCustomerType;

        public DefaultWrapperExecutor()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public object WrapFailedResult(object originalData, DataWrapperContext wrapperContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public object WrapSuccesfullysResult(object orignalData, DataWrapperContext wrapperContext)
        {
            if (orignalData is IResultDataWrapper)
                return orignalData;

            var httpContext = wrapperContext.HttpContext;
            var options = wrapperContext.WrapperOptions;

            if (!options.UseCustomModel)
            {
                return new ApiResponse(message: $"[{httpContext.Request.Method}] {ResponseMessage.Success}",
                                      result: orignalData,
                                      statusCode: wrapperContext.HttpContext.Response.StatusCode);
            }
            else
            {
                //create customer model.
                var customerType = CreateCustomerModel(wrapperContext);
                var modelInstance = Activator.CreateInstance(customerType);

                foreach (var customerProperty in options.CustomerProperty)
                {
                    var propertyValue = customerProperty.Value(wrapperContext);
                    customerType.GetProperty(customerProperty.Key).SetValue(modelInstance, propertyValue);
                }
                return modelInstance;
            }
        }

        protected virtual Type CreateCustomerModel(DataWrapperContext wrapperContext)
        {
            if (_cacheCustomerType != null)
                return _cacheCustomerType;

            var dyClass = EmitHelper.CreateClass("MiCakeWrapperResponse",
                                     MiCakeReflectionPredefined.DynamicAssembly,
                                     MiCakeReflectionPredefined.DynamicModule,
                                     baseType: typeof(BaseResultDataWrapper));

            foreach (var customerProperty in wrapperContext.WrapperOptions.CustomerProperty)
            {
                CheckValue.NotNullOrEmpty(customerProperty.Key, nameof(customerProperty.Key));

                EmitHelper.CreateProperty(dyClass, customerProperty.Key, typeof(string));
            }

            return _cacheCustomerType = dyClass.CreateType();
        }
    }
}
