using MiCake.Core;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// The executor for data wrapper.
    /// </summary>
    public interface IDataWrapperExecutor
    {
        /// <summary>
        /// Wrap normal API response data.
        /// </summary>
        /// <param name="orignalData">Original data.</param>
        /// <param name="wrapperContext"><see cref="DataWrapperContext"/></param>
        /// <returns>wrapped data</returns>
        object WrapApiResponse(object orignalData, DataWrapperContext wrapperContext);

        /// <summary>
        /// Wrap some exception inherit from <see cref="PureException"/>.
        /// </summary>
        /// <param name="exception">exception info</param>
        /// <param name="wrapperContext"><see cref="DataWrapperContext"/></param>
        /// <returns>wrapped data</returns>
        object WrapPureException(PureException exception, DataWrapperContext wrapperContext);
    }
}
