using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// A provider for get <see cref="DbContext"/>
    /// </summary>
    public interface IDbContextProvider
    {
        Task<DbContext> GetDbContextAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A provider for get <see cref="DbContext"/>
    /// </summary>
    /// <typeparam name="TDbContext">Type of DbContext</typeparam>
    public interface IDbContextProvider<TDbContext> : IDbContextProvider where TDbContext : DbContext
    {
        new Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default);
    }
}
