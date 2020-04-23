using Microsoft.AspNetCore.Mvc;
using System;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// The executor for data wrapper.
    /// </summary>
    public interface IDataWrapperExecutor
    {
        /// <summary>
        /// Wrap successfully returned data
        /// </summary>
        /// <param name="orignalData">Original data.For Aspnet Core,it's always <see cref="ObjectResult"/></param>
        /// <param name="wrapperContext"><see cref="DataWrapperContext"/></param>
        /// <returns>wrapped data</returns>
        object WrapSuccesfullysResult(object orignalData, DataWrapperContext wrapperContext);

        /// <summary>
        /// Wrap the data returned by the error
        /// </summary>
        /// <param name="originalData">Original data.For Aspnet Core,it's always <see cref="ObjectResult"/></param>
        /// <param name="exception">exception info</param>
        /// <param name="wrapperContext"><see cref="DataWrapperContext"/></param>
        /// <returns>wrapped data</returns>
        object WrapFailedResult(object originalData, Exception exception, DataWrapperContext wrapperContext);
    }
}
