using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Use efcore as ORM and readonly repository with persistent objects.
    /// </summary>
    /// <typeparam name="TDbContext">Type Of DBContext</typeparam>
    /// <typeparam name="TAggregateRoot">Type of <see cref="IAggregateRoot"/></typeparam>
    /// <typeparam name="TPersistentObject">Type of <see cref="PersistentObject{TEntity}"/></typeparam>
    /// <typeparam name="TKey">Primary key type of <see cref="IAggregateRoot"/></typeparam>
    public class EFReadOnlyRepositoryWithPO<TDbContext, TAggregateRoot, TPersistentObject, TKey> :
        EFRepositoryBase<TDbContext, TAggregateRoot, TKey>,
        IReadOnlyRepository<TAggregateRoot, TKey>, IDisposable
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasPersistentObject
        where TPersistentObject : class, IPersistentObject
        where TDbContext : DbContext
    {
        protected new DbSet<TPersistentObject> DbSet => DbContext.Set<TPersistentObject>();

        public EFReadOnlyRepositoryWithPO(IDbContextProvider<TDbContext> dbContextProvider) : base(dbContextProvider)
        {

        }

        public TAggregateRoot Find(TKey ID)
        {
            var snapshotModel = DbContext.Find<TPersistentObject>(ID);
            return default;
        }

        public virtual async Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var snapshotModel = await DbContext.FindAsync<TPersistentObject>(new object[] { ID }, cancellationToken);
            return default;
        }

        public virtual long GetCount()
        {
            return DbSet.CountAsync().Result;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
