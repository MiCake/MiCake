using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// A read-only EF Core Repository base class.
    /// </summary>
    public class EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey> :
        EFRepositoryBase<TDbContext, TAggregateRoot, TKey>,
        IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
    {
        public EFReadOnlyRepository(IDbContextProvider<TDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public TAggregateRoot Find(TKey ID)
        {
            return DbContext.Find<TAggregateRoot>(ID);
        }

        public async Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            return await DbContext.FindAsync<TAggregateRoot>(new object[] { ID }, cancellationToken);
        }

        public long GetCount()
        {
            return DbSet.CountAsync().Result;
        }
    }
}
