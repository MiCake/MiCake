using MiCake.Core.Util;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// A read-only EF Core Repository base class.
    /// </summary>
    public abstract class EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey> :
        EFRepositoryBase<TDbContext, TAggregateRoot, TKey>,
        IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
    {
        public EFReadOnlyRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }


        public virtual async ValueTask<TAggregateRoot?> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNull(ID, nameof(ID));

            return await DbSet.FindAsync(new object[] { ID! }, cancellationToken);
        }

        public virtual async ValueTask<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet.LongCountAsync(cancellationToken);
        }
    }
}
