using Mapster;
using MiCake.Core.Util.Collections;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
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
    /// <typeparam name="TPersistentObject">Type of <see cref="PersistentObject{TEntity}"/></typeparam>
    /// <typeparam name="TKey">Primary key type of <see cref="IAggregateRoot"/></typeparam>
    public class EFReadOnlyRepositoryWithPO<TDbContext, TAggregateRoot, TPersistentObject, TKey> :
        IReadOnlyRepository<TAggregateRoot, TKey>, IDisposable
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasPersistentObject
        where TPersistentObject : class, IPersistentObject
        where TDbContext : DbContext
    {
        protected virtual TDbContext DbContext => _dbContextFactory.CreateDbContext();
        protected virtual DbSet<TPersistentObject> DbSet => DbContext.Set<TPersistentObject>();

        //the relationship between entity instance and persistent object instancae.
        private ConcurrentDictionary<object, object> _entityRelationship = new ConcurrentDictionary<object, object>();

        private readonly IUnitOfWorkManager _uowManager;
        private IUowDbContextFactory<TDbContext> _dbContextFactory;

        public EFReadOnlyRepositoryWithPO(IUnitOfWorkManager uowManager)
        {
            _uowManager = uowManager;

            _dbContextFactory = new UowDbContextFactory<TDbContext>(_uowManager);
        }

        public TAggregateRoot Find(TKey ID)
        {
            var snapshotModel = DbContext.Find<TPersistentObject>(ID);
            return ToEntity(snapshotModel);
        }

        public async Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var snapshotModel = await DbContext.FindAsync<TPersistentObject>(ID);
            return ToEntity(snapshotModel);
        }

        public long GetCount()
        {
            return DbSet.CountAsync().Result;
        }

        protected virtual TAggregateRoot ToEntity(TPersistentObject snapshot)
        {
            TAggregateRoot result;

            var key = _entityRelationship.GetFirstKeyByValue(snapshot);
            if (key != null)
            {
                result = snapshot.Adapt((TAggregateRoot)key);
            }
            else
            {
                result = snapshot.Adapt<TAggregateRoot>();
                _entityRelationship.TryAdd(result, snapshot);
            }

            return result;
        }

        protected virtual List<TAggregateRoot> ToEntity(List<TPersistentObject> snapshot)
        {
            List<TAggregateRoot> result;

            var key = _entityRelationship.GetFirstKeyByValue(snapshot);
            if (key != null)
            {
                result = snapshot.Adapt((List<TAggregateRoot>)key);
            }
            else
            {
                result = snapshot.Adapt<List<TAggregateRoot>>();
                _entityRelationship.TryAdd(result, snapshot);
            }

            return result;
        }

        protected virtual TPersistentObject ToPersistentObject(TAggregateRoot aggregateRoot)
        {
            TPersistentObject persistentObject;

            if (_entityRelationship.TryGetValue(aggregateRoot, out var model))
            {
                TPersistentObject convertModel = (TPersistentObject)model;
                persistentObject = aggregateRoot.Adapt(convertModel);
            }
            else
            {
                persistentObject = aggregateRoot.Adapt<TPersistentObject>();
                _entityRelationship.TryAdd(aggregateRoot, persistentObject);
            }

            return persistentObject;
        }

        protected virtual List<TPersistentObject> ToPersistentObject(List<TAggregateRoot> aggregateRoot)
        {
            List<TPersistentObject> persistentObjects;

            if (_entityRelationship.TryGetValue(aggregateRoot, out var model))
            {
                persistentObjects = aggregateRoot.Adapt((List<TPersistentObject>)model);
            }
            else
            {
                persistentObjects = aggregateRoot.Adapt<List<TPersistentObject>>();
                _entityRelationship.TryAdd(aggregateRoot, persistentObjects);
            }

            return persistentObjects;
        }

        public void Dispose()
        {
            if (_entityRelationship.Count > 0)
                _entityRelationship.Clear();

            _entityRelationship = null;
        }
    }
}
