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
    public class EFStorageModelReadOnlyRepository<TDbContext, TAggregateRoot, TStorageModel, TKey> :
        IReadOnlyRepository<TAggregateRoot, TKey>, IDisposable
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasStorageModel
        where TStorageModel : class, IStorageModel
        where TDbContext : DbContext
    {
        protected virtual TDbContext DbContext => _dbContextFactory.CreateDbContext();
        protected virtual DbSet<TStorageModel> DbSet => DbContext.Set<TStorageModel>();

        //the relationship between entity instance and storage model instancae.
        private ConcurrentDictionary<object, object> _entityRelationship = new ConcurrentDictionary<object, object>();

        private readonly IUnitOfWorkManager _uowManager;
        private IUowDbContextFactory<TDbContext> _dbContextFactory;

        public EFStorageModelReadOnlyRepository(IUnitOfWorkManager uowManager)
        {
            _uowManager = uowManager;

            _dbContextFactory = new UowDbContextFactory<TDbContext>(_uowManager);
        }

        public TAggregateRoot Find(TKey ID)
        {
            var snapshotModel = DbContext.Find<TStorageModel>(ID);
            return ToEntity(snapshotModel);
        }

        public async Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var snapshotModel = await DbContext.FindAsync<TStorageModel>(ID);
            return ToEntity(snapshotModel);
        }

        public long GetCount()
        {
            return DbSet.CountAsync().Result;
        }

        protected virtual TAggregateRoot ToEntity(TStorageModel snapshot)
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

        protected virtual List<TAggregateRoot> ToEntity(List<TStorageModel> snapshot)
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

        protected virtual TStorageModel ToStorageModel(TAggregateRoot aggregateRoot)
        {
            TStorageModel storageModel;

            if (_entityRelationship.TryGetValue(aggregateRoot, out var model))
            {
                TStorageModel convertModel = (TStorageModel)model;
                storageModel = aggregateRoot.Adapt(convertModel);
            }
            else
            {
                storageModel = aggregateRoot.Adapt<TStorageModel>();
                _entityRelationship.TryAdd(aggregateRoot, storageModel);
            }

            return storageModel;
        }

        protected virtual List<TStorageModel> ToStorageModel(List<TAggregateRoot> aggregateRoot)
        {
            List<TStorageModel> storageModel;

            if (_entityRelationship.TryGetValue(aggregateRoot, out var model))
            {
                storageModel = aggregateRoot.Adapt((List<TStorageModel>)model);
            }
            else
            {
                storageModel = aggregateRoot.Adapt<List<TStorageModel>>();
                _entityRelationship.TryAdd(aggregateRoot, storageModel);
            }

            return storageModel;
        }

        public void Dispose()
        {
            if (_entityRelationship.Count > 0)
                _entityRelationship.Clear();

            _entityRelationship = null;
        }
    }
}
