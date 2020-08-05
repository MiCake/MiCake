using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// A provider for get <see cref="DbContext"/>
    /// </summary>
    public interface IDbContextProvider
    {
        DbContext GetDbContext();
    }

    /// <summary>
    /// A provider for get <see cref="DbContext"/>
    /// </summary>
    /// <typeparam name="TDbContext">Type of DbContext</typeparam>
    public interface IDbContextProvider<out TDbContext> : IDbContextProvider where TDbContext : DbContext
    {
        new TDbContext GetDbContext();
    }
}
