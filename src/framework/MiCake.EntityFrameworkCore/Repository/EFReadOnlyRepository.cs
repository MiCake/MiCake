using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
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
        public EFReadOnlyRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }


        public virtual async Task<TAggregateRoot> FindAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            return await dbset.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            return await dbset.LongCountAsync(cancellationToken);
        }
    }
}
