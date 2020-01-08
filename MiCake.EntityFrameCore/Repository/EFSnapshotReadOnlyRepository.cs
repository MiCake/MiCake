using MiCake.DDD.Domain;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    public class EFSnapshotReadOnlyRepository<TDbContext, TAggregateRoot, TSnapshot, TKey> :
        IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>, IEntityHasSnapshot<TSnapshot>
        where TSnapshot : class
        where TDbContext : DbContext
    {
        protected virtual TDbContext DbContext => _dbContextFactory.CreateDbContext();
        protected virtual DbSet<TSnapshot> DbSet => DbContext.Set<TSnapshot>();

        private readonly IUnitOfWorkManager _uowManager;
        private IUowDbContextFactory<TDbContext> _dbContextFactory;

        public EFSnapshotReadOnlyRepository(IUnitOfWorkManager uowManager)
        {
            _uowManager = uowManager;

            _dbContextFactory = new UowDbContextFactory<TDbContext>(_uowManager);
        }

        public TAggregateRoot Find(TKey ID)
        {
            var snapshotModel = DbContext.Find<TSnapshot>(ID);
            return CreateFromSnapshot(snapshotModel);
        }

        public async Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var snapshotModel = await DbContext.FindAsync<TSnapshot>(ID);
            return CreateFromSnapshot(snapshotModel);
        }

        public long GetCount()
        {
            return DbSet.CountAsync().Result;
        }

        protected virtual TAggregateRoot CreateFromSnapshot(TSnapshot snapshot)
        {
            return (TAggregateRoot)Activator.CreateInstance(typeof(TAggregateRoot), snapshot);
        }
    }
}
