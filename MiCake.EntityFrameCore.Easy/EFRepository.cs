using MiCake.DDD.Domain;
using MiCake.Uow.Easy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameCore.Easy
{
    public class EFRepository<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        protected IUnitOfWorkManager UnitOfWorkManager { get; private set; }
        protected DbContext DbContext { get; private set; }

        public EFRepository(IUnitOfWorkManager unitOfWorkManager, DbContext dbContext)
        {
            UnitOfWorkManager = unitOfWorkManager;
            DbContext = dbContext;
        }

        public void Add(TAggregateRoot aggregateRoot)
        {
            RegistUnitOfWork(DbContext);

            DbContext.Set<TAggregateRoot>().Add(aggregateRoot);
        }

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Delete(TAggregateRoot aggregateRoot)
        {
            RegistUnitOfWork(DbContext);

            DbContext.Set<TAggregateRoot>().Remove(aggregateRoot);
        }

        public void DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot Find(TKey ID)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public long GetCount()
        {
            throw new NotImplementedException();
        }

        public void Update(TAggregateRoot aggregateRoot)
        {
            RegistUnitOfWork(DbContext);

            DbContext.Set<TAggregateRoot>().Update(aggregateRoot);
        }

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private void RegistUnitOfWork(DbContext dbContext)
        {
            string key = $"EFTranscationFeature - {dbContext.ContextId.InstanceId.ToString()}";
            UnitOfWorkManager.Create(default).ResigtedTranscationFeature(key, new EFTranscationFeature(DbContext));
        }
    }
}
