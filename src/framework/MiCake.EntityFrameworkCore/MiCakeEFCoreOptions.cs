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
        /// Gets or sets a value indicating whether DbContext can be resolved through
        /// <see cref="IRepository"/> or the context factory without an active Unit of Work (UoW) scope.
        /// <para>
        /// When set to <c>true</c>, DbContext access is allowed when no UoW is active.
        /// This is useful for scenarios such as:
        /// <list type="bullet">
        ///   <item><description>Middleware-level data access for configuration or authorization</description></item>
        ///   <item><description>Health check endpoints that only perform read operations</description></item>
        ///   <item><description>Background services that need temporary read-only access</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>WRITE POLICY IS PERMISSIVE:</b> this option only relaxes context resolution. The write
        /// guard applies inside an ambient writable UoW; without one, framework-mediated write paths
        /// (SaveChanges, bulk operations, raw SQL) pass through with native EF semantics — no
        /// transaction or lifecycle guarantee applies.
        /// </para>
        /// <para>
        /// <b>LIMITATIONS:</b>
        /// <list type="bullet">
        ///   <item><description>Domain events will not be dispatched automatically (no UoW to trigger SaveChanges)</description></item>
        ///   <item><description>MiCake features (audit, domain event, etc.) may not work correctly</description></item>
        ///   <item><description>Multiple repository operations outside a UoW are not atomic (partial success/failure possible)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>RECOMMENDATION:</b> Prefer an explicit <c>IStandaloneUnitOfWorkExecutor</c> boundary when
        /// persistence is required; use this option only for read-only query scenarios.
        /// </para>
        /// <para>Default: <c>false</c> (UoW check is enforced)</para>
        /// </summary>
        public bool AllowDbContextAccessWithoutUoW { get; set; } = false;

        /// <summary>
        /// The maximum number of save cycles a root save operation may execute while
        /// handling controlled SaveChanges re-entry requests from lifecycle or
        /// domain-event handlers. When the limit is reached, the save operation throws
        /// <see cref="MiCake.DDD.Uow.Exceptions.SaveChangesReentryException"/> and the unit
        /// of work is left rollback-only.
        /// </summary>
        public int MaxSaveCycles { get; set; } = 16;

        MiCakeEFCoreOptions IObjectAccessor<MiCakeEFCoreOptions>.Value => this;

        public MiCakeEFCoreOptions(Type dbContextType)
        {
            DbContextType = dbContextType ?? throw new ArgumentNullException(nameof(dbContextType));
        }
    }
}
