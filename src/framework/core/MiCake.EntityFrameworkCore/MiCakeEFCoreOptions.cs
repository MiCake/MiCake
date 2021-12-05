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

        MiCakeEFCoreOptions IObjectAccessor<MiCakeEFCoreOptions>.Value => this;

        public MiCakeEFCoreOptions(Type dbContextType)
        {
            DbContextType = dbContextType ?? throw new ArgumentNullException($"{nameof(MiCakeEFCoreOptions.DbContextType)} can not be null.");
        }
    }
}
