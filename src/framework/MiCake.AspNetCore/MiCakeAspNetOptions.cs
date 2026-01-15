using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.Responses;
using MiCake.DDD.Uow;
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
        public MiCakeAspNetUowOptions UnitOfWork { get; set; }

        /// <summary>
        /// Whether it is needed to format the returned data.
        /// When you choose true, you can also customize the configuration by <see cref="DataWrapperOptions"/>
        /// </summary>
        public bool UseDataWrapper { get; set; } = true;

        /// <summary>
        /// The data wrap configuration for MiCake in ASP.NET Core.
        /// </summary>
        public ResponseWrapperOptions DataWrapperOptions { get; set; }

        /// <summary>
        /// Whether to enable API request/response logging.
        /// When true, HTTP requests and responses will be logged for debugging, auditing, and monitoring.
        /// Configure logging behavior via <see cref="ApiLoggingOptions"/>.
        /// <para>
        /// Default: false
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// services.AddMiCake(typeof(MyModule))
        ///     .UseAspNetCore(options =>
        ///     {
        ///         options.UseApiLogging = true;
        ///         options.ApiLoggingOptions.ExcludeStatusCodes = [200, 204];
        ///         options.ApiLoggingOptions.SensitiveFields.Add("phoneNumber");
        ///     })
        ///     .Build();
        /// </code>
        /// </example>
        public bool UseApiLogging { get; set; } = false;

        /// <summary>
        /// The API logging configuration for MiCake in ASP.NET Core.
        /// Only takes effect when <see cref="UseApiLogging"/> is set to true.
        /// </summary>
        public ApiLoggingOptions ApiLoggingOptions { get; set; }

        public MiCakeAspNetOptions()
        {
            UnitOfWork = new MiCakeAspNetUowOptions();
            DataWrapperOptions = new ResponseWrapperOptions();
            ApiLoggingOptions = new ApiLoggingOptions();
        }
    }

    /// <summary>
    /// Provides configuration for the MiCake Unit of Work in ASP.NET Core.
    /// Allows automatic UoW management for controller actions.
    /// </summary>
    public class MiCakeAspNetUowOptions
    {
        /// <summary>
        /// Enables automatic Unit of Work management for controller actions.
        /// When true, a Unit of Work is automatically created before action execution,
        /// and committed after successful execution or rolled back on failure.
        /// <para>
        /// It will be use <see cref="UnitOfWorkOptions.Default"/> as the default configuration for created UoW.
        /// </para>
        /// <para>
        /// Default: true. This can be overridden at the Controller or Action level using [UnitOfWork] attribute.
        /// </para>
        /// </summary>
        public bool EnableAutoUnitOfWork { get; set; } = true;

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
