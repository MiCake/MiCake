using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Mark this <see cref="IDbExecutor"/> is for EFCore.
    /// </summary>
    public interface IEFCoreDbExecutor
    {
        /// <summary>
        /// Current <see cref="DbContext"/>.
        /// </summary>
        DbContext EFCoreDbContext { get; }

        /// <summary>
        /// The indicator whether to only use efcore savechanges method to save changes for uow and won't use commit.
        /// </summary>
        bool ShouldCommitTrxForEFCore { get; }
    }
}
