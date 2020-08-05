using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using MiCake.EntityFrameworkCore.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Use efcore as ORM and readonly repository with persistent objects.
    /// </summary>
    /// <typeparam name="TDbContext">Type Of DBContext</typeparam>
    /// <typeparam name="TAggregateRoot">Type of <see cref="IAggregateRoot"/></typeparam>
    /// <typeparam name="TPersistentObject">Type of <see cref="PersistentObject{TKey,TEntity, TPersistentObject}"/></typeparam>
    /// <typeparam name="TKey">Primary key type of <see cref="IAggregateRoot"/></typeparam>
    public class EFReadOnlyRepositoryWithPO<TDbContext, TAggregateRoot, TPersistentObject, TKey> :
        EFRepositoryBase<TDbContext, TAggregateRoot, TKey>,
        IReadOnlyRepository<TAggregateRoot, TKey>,
        IDisposable
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasPersistentObject
        where TPersistentObject : class, IPersistentObject
        where TDbContext : DbContext
    {
        protected new DbSet<TPersistentObject> DbSet => DbContext.Set<TPersistentObject>();
        protected EFCorePoManager<TAggregateRoot, TPersistentObject> POManager { get; private set; }

        public EFReadOnlyRepositoryWithPO(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        public TAggregateRoot Find(TKey ID)
        {
            var POInstance = DbContext.Find<TPersistentObject>(ID);
            return POManager.MapToDO(POInstance);
        }

        public virtual async Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var POInstance = await DbContext.FindAsync<TPersistentObject>(new object[] { ID }, cancellationToken);
            return POManager.MapToDO(POInstance);
        }

        public virtual long GetCount()
        {
            return DbSet.CountAsync().Result;
        }

        public void Dispose()
        {
            POManager.Dispose();
            POManager = null;
        }

        protected override void InitComponents()
        {
            base.InitComponents();
            POManager = ServiceProvider.GetService<EFCorePoManager<TAggregateRoot, TPersistentObject>>();
        }

        protected virtual TPersistentObject MapToPO(TAggregateRoot aggregateRoot)
        {
            return POManager.MapToPO(aggregateRoot);
        }

        protected virtual IEnumerable<TPersistentObject> MapToPO(IEnumerable<TAggregateRoot> aggregateRoot)
        {
            return POManager.MapToPO(aggregateRoot);
        }

        protected virtual TAggregateRoot MapToDO(TPersistentObject aggregateRoot)
        {
            return POManager.MapToDO(aggregateRoot);
        }

        protected virtual IEnumerable<TAggregateRoot> MapToDO(IEnumerable<TPersistentObject> aggregateRoot)
        {
            return POManager.MapToDO(aggregateRoot);
        }
    }
}
