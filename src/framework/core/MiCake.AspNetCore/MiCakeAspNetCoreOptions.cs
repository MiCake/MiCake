using MiCake.AspNetCore.DataWrapper;
using MiCake.Core;
using MiCake.Core.Data;
using MiCake.Uow;

namespace MiCake.AspNetCore
{
    /// <summary>
    /// The options for micake asp net core.
    /// </summary>
    public class MiCakeAspNetCoreOptions : ICanApplyData<MiCakeAspNetCoreOptions>
    {
        /// <summary>
        /// Indicate need open <see cref="AutoUnitOfWorkAttribute"/> in all controller.
        /// if value is true, all controller will be influenced by <see cref="AutoUnitOfWorkAttribute"/>.
        /// 
        /// <para>
        ///     default value is true. It's mean each Actions will savechanges automatically.
        /// </para>
        /// </summary>
        public bool GlobalAutoUowInController { get; set; } = true;

        /// <summary>
        /// Indicate need wrap API response data and <see cref="PureException"/>(or any exception inherit from <see cref="PureException"/>) to <see cref="ApiResponse"/>.
        /// 
        /// <para>
        ///     defalut value is true.
        /// </para>
        /// </summary>
        public bool WrapResponseAndPureExceptionData { get; set; } = true;

        public MiCakeAspNetCoreOptions()
        {
        }

        public void Apply(MiCakeAspNetCoreOptions options)
        {
            GlobalAutoUowInController = options.GlobalAutoUowInController;
            WrapResponseAndPureExceptionData = options.WrapResponseAndPureExceptionData;
        }
    }
}
