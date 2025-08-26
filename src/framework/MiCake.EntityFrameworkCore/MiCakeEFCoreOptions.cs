using MiCake.Core.DependencyInjection;
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

        public bool WillOpenTransactionForUow { get; set; } = false;

        /// <summary>
        /// Whether to use implicit mode for Unit of Work.
        /// If the <see cref="ImplicitModeForUow"/> is false, you must open a uow before using the repository.
        /// <para>
        /// Default: true
        /// </para>
        /// </summary>
        public bool ImplicitModeForUow { get; set; } = true;

        MiCakeEFCoreOptions IObjectAccessor<MiCakeEFCoreOptions>.Value => this;

        public MiCakeEFCoreOptions(Type dbContextType)
        {
            DbContextType = dbContextType ?? throw new ArgumentNullException($"{nameof(DbContextType)} can not be null.");
        }
    }
}
