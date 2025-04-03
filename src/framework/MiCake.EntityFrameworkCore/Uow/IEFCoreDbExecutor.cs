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
    }
}
