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
        /// <param name="orignalData">Original data.</param>
        /// <param name="isSoftException">orignal data is soft exception</param>
        /// <param name="wrapperContext"><see cref="DataWrapperContext"/></param>
        /// <returns>wrapped data</returns>
        object WrapSuccesfullysResult(object orignalData, DataWrapperContext wrapperContext, bool isSoftException = false);

        /// <summary>
        /// Wrap the data returned by the error
        /// </summary>
        /// <param name="originalData">Original data.</param>
        /// <param name="exception">exception info</param>
        /// <param name="wrapperContext"><see cref="DataWrapperContext"/></param>
        /// <returns>wrapped data</returns>
        object WrapFailedResult(object originalData, Exception exception, DataWrapperContext wrapperContext);
    }
}
