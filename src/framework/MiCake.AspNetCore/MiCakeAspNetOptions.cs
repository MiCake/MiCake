using MiCake.AspNetCore.DataWrapper;
using System.Collections.Generic;

namespace MiCake.AspNetCore
{
    /// <summary>
    /// The options for micake asp net core.
    /// </summary>
    public class MiCakeAspNetOptions
    {
        /// <summary>
        /// The unit of work config for micake in asp net core.
        /// </summary>
        public MiCakeAspNetUowOption UnitOfWork { get; set; }

        /// <summary>
        /// Whether it is need to format the returned data.
        /// When you choose true, you can also customize the configuration by <see cref="DataWrapperOptions"/>
        /// </summary>
        public bool UseDataWrapper { get; set; } = true;

        /// <summary>
        /// The data wrap config for micake in asp net core.
        /// </summary>
        public DataWrapperOptions DataWrapperOptions { get; set; }

        public MiCakeAspNetOptions()
        {
            UnitOfWork = new MiCakeAspNetUowOption();
            DataWrapperOptions = new DataWrapperOptions();
        }
    }

    /// <summary>
    /// Provides configuration for the MiCake UnitOfWork.
    /// </summary>
    public class MiCakeAspNetUowOption
    {
        /// <summary>
        /// Match controller action name start key work to close unit of work transaction to improve performance.
        /// <para>
        /// Default: [Find],[Get],[Query],[Search]
        /// </para>
        /// </summary>
        public List<string> KeyWordForCloseAutoCommit { get; set; } = ["Find", "Get", "Query", "Search"];
    }
}
