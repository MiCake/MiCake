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
        /// Enables automatic creation of Unit of Work for each controller action.
        /// When true, a new UoW is automatically started before action execution.
        /// Default: true
        /// </summary>
        public bool IsAutoBeginEnabled { get; set; } = true;

        /// <summary>
        /// Enables automatic commit of Unit of Work after successful action execution.
        /// When true, UoW is committed automatically if the action completes without exceptions.
        /// When false, you must manually commit the UoW in your action methods.
        /// Default: true
        /// </summary>
        public bool IsAutoCommitEnabled { get; set; } = true;

        /// <summary>
        /// Match controller action name start keywords to skip auto-commit for read-only operations.
        /// Actions starting with these keywords will have their UoW marked as completed without commit,
        /// which improves performance for read-only operations.
        /// <para>
        /// Default: [Find, Get, Query, Search]
        /// </para>
        /// </summary>
        public List<string> KeyWordForCloseAutoCommit { get; set; } = ["Find", "Get", "Query", "Search"];

        /// <summary>
        /// Enables automatic rollback of Unit of Work when action execution fails.
        /// When true and an exception occurs, the UoW will be rolled back automatically.
        /// Default: true
        /// </summary>
        public bool IsAutoRollbackEnabled { get; set; } = true;
    }
}
