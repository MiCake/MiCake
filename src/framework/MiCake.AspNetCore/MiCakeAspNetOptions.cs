using MiCake.AspNetCore.DataWrapper;
using System.Collections.Generic;

namespace MiCake.AspNetCore
{
    /// <summary>
    /// The options for MiCake ASP.NET Core integration.
    /// </summary>
    public class MiCakeAspNetOptions
    {
        /// <summary>
        /// The unit of work configuration for MiCake in ASP.NET Core.
        /// </summary>
        public MiCakeAspNetUowOption UnitOfWork { get; set; }

        /// <summary>
        /// Whether it is needed to format the returned data.
        /// When you choose true, you can also customize the configuration by <see cref="DataWrapperOptions"/>
        /// </summary>
        public bool UseDataWrapper { get; set; } = true;

        /// <summary>
        /// The data wrap configuration for MiCake in ASP.NET Core.
        /// </summary>
        public DataWrapperOptions DataWrapperOptions { get; set; }

        public MiCakeAspNetOptions()
        {
            UnitOfWork = new MiCakeAspNetUowOption();
            DataWrapperOptions = new DataWrapperOptions();
        }
    }

    /// <summary>
    /// Provides configuration for the MiCake Unit of Work in ASP.NET Core.
    /// Allows automatic UoW management for controller actions.
    /// </summary>
    public class MiCakeAspNetUowOption
    {
        /// <summary>
        /// Enables automatic transaction management for controller actions.
        /// When true, a Unit of Work is automatically created before action execution,
        /// and committed after successful execution or rolled back on failure.
        /// When false, you must manually manage transactions using IUnitOfWorkManager.
        /// Default: true
        /// 
        /// <para>
        /// This can be overridden at the Controller or Action level using [UnitOfWork] attribute.
        /// </para>
        /// </summary>
        public bool IsAutoTransactionEnabled { get; set; } = true;

        /// <summary>
        /// Match controller action name start keywords to treat actions as read-only operations.
        /// Actions starting with these keywords will have their UoW marked as read-only,
        /// which improves performance by skipping transaction commit.
        /// <para>
        /// Default: [Find, Get, Query, Search]
        /// </para>
        /// </summary>
        public List<string> ReadOnlyActionKeywords { get; set; } = ["Find", "Get", "Query", "Search"];
    }
}
