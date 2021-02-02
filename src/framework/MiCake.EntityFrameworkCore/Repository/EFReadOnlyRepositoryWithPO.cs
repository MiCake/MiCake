using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
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
        IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasPersistentObject
        where TPersistentObject : class, IPersistentObject<TKey, TAggregateRoot>
        where TDbContext : DbContext
    {
        protected new DbSet<TPersistentObject> DbSet => DbContext.Set<TPersistentObject>();
        protected IPersistentObjectMapper Mapper { get; private set; }

        /// <summary>
        /// DbSetQuery is no tracking model for current DbSet.
        /// <para>
        /// PO repository should use no tracking model to get data. Because change of data occurs in domain object,not persistent data.
        /// </para>
        /// </summary>
        protected IQueryable<TPersistentObject> DbSetQuery => DbSet.AsNoTracking();

        public EFReadOnlyRepositoryWithPO(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public TAggregateRoot Find(TKey ID)
        {
            var POInstance = DbSetQuery.FirstOrDefaultAsync(s => s.Id.Equals(ID)).GetAwaiter().GetResult();
            return Convert(POInstance);
        }

        public virtual async Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var POInstance = await DbSetQuery.FirstOrDefaultAsync(s => s.Id.Equals(ID), cancellationToken);
            return Convert(POInstance);
        }

        public virtual long GetCount()
        {
            return DbSet.CountAsync().Result;
        }

        protected override void InitComponents()
        {
            base.InitComponents();
            Mapper = ServiceProvider.GetService<IPersistentObjectMapper>();
        }

        /// <summary>
        /// Convert persistent object to domain object.
        /// </summary>
        /// <param name="persistentObject"></param>
        /// <returns></returns>
        protected virtual TAggregateRoot Convert(TPersistentObject persistentObject)
            => Mapper.ToDomainEntity<TAggregateRoot, TPersistentObject>(persistentObject);

        /// <summary>
        /// Convert persistent object to domain object.(list)
        /// </summary>
        /// <param name="persistentObjects"></param>
        /// <returns></returns>
        protected virtual IEnumerable<TAggregateRoot> Convert(IEnumerable<TPersistentObject> persistentObjects)
            => Mapper.Map<IEnumerable<TPersistentObject>, IEnumerable<TAggregateRoot>>(persistentObjects);

        /// <summary>
        /// Convert domain object to persistent object.
        ///  <para>
        ///     If <paramref name="autoUpdateEntry"/> is not specified, the entity state is automatically updated.
        /// </para>
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <param name="autoUpdateEntry">is auto update efcore entry.if value is true,will call DbContext.Update() automatic.</param>
        /// <returns></returns>
        protected virtual TPersistentObject ReverseConvert(TAggregateRoot aggregateRoot, bool autoUpdateEntry = true)
        {
            var persistentObj = Mapper.ToPersistentObject<TAggregateRoot, TPersistentObject>(aggregateRoot);
            if (autoUpdateEntry)
            {
                DbContext.Update(persistentObj);
            }
            return persistentObj;
        }

        /// <summary>
        /// Convert domain object to persistent object.(list)
        /// <para>
        ///     If <paramref name="autoUpdateEntry"/> is not specified, the entity state is automatically updated.
        /// </para>
        /// </summary>
        /// <param name="aggregateRoots"></param>
        /// <param name="autoUpdateEntry">is auto update efcore entry.if value is true,will call DbContext.Update() automatic.</param>
        /// <returns></returns>
        protected virtual IEnumerable<TPersistentObject> ReverseConvert(IEnumerable<TAggregateRoot> aggregateRoots, bool autoUpdateEntry = true)
        {
            var persistentObjs = Mapper.Map<IEnumerable<TAggregateRoot>, IEnumerable<TPersistentObject>>(aggregateRoots);
            if (autoUpdateEntry)
            {
                DbContext.UpdateRange(persistentObjs);
            }
            return persistentObjs;
        }
    }
}
