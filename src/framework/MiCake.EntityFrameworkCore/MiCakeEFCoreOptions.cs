using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using System;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// The options of EFCore extension for MiCake.
    /// </summary>
    public class MiCakeEFCoreOptions : IObjectAccessor<MiCakeEFCoreOptions>
    {
        /// <summary>
        /// Type of <see cref="MiCakeDbContext"/>.
        /// </summary>
        public Type DbContextType { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to bypass the Unit of Work (UoW) check
        /// when instantiating repositories or accessing DbContext through <see cref="IRepository"/>.
        /// <para>
        /// When set to <c>true</c>, DbContext can be accessed without requiring an active Unit of Work scope.
        /// This is useful for scenarios such as:
        /// <list type="bullet">
        ///   <item><description>Middleware-level data access for configuration or authorization</description></item>
        ///   <item><description>Health check endpoints that only perform read operations</description></item>
        ///   <item><description>Background services that need temporary read-only access</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>RISKS AND LIMITATIONS:</b>
        /// <list type="bullet">
        ///   <item><description>Domain events will NOT be dispatched automatically (no UoW to trigger SaveChanges)</description></item>
        ///   <item><description>MiCake features (audit,domain event,etc..) may not work correctly</description></item>
        ///   <item><description>Transaction management is not available - each operation commits independently</description></item>
        ///   <item><description>Multiple repository operations will NOT be atomic (partial success/failure possible)</description></item>
        ///   <item><description>Rollback capability is not available for write operations</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>RECOMMENDATION:</b> Only set to <c>true</c> for read-only query scenarios.
        /// For write operations, always use proper Unit of Work scope.
        /// </para>
        /// <para>Default: <c>false</c> (UoW check is enforced)</para>
        /// </summary>
        public bool BypassUnitOfWorkCheck { get; set; } = false;

        MiCakeEFCoreOptions IObjectAccessor<MiCakeEFCoreOptions>.Value => this;

        public MiCakeEFCoreOptions(Type dbContextType)
        {
            DbContextType = dbContextType ?? throw new ArgumentNullException($"{nameof(DbContextType)} can not be null.");
        }
    }
}
