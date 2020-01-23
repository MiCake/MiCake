using JetBrains.Annotations;
using MiCake.Core.DependencyInjection;
using MiCake.DDD.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore
{
    public class MiCakeDbContext : DbContext
    {
        private List<IRepositoryLifetime> _repositoryLifetimes;

        public MiCakeDbContext([NotNull] DbContextOptions options) : base(options)
        {
            var lifetimes = ServiceLocator.Instance.GetSerivces<IRepositoryLifetime>();

            _repositoryLifetimes = lifetimes.Count() == 0 ?
                new List<IRepositoryLifetime>() : 
                lifetimes.Where(s => s is IEfRepositoryLifetime).ToList();
        }

        public override int SaveChanges()
        {
            int saveResult;

            ApplyPreSaveChangeLifetime();

            saveResult = base.SaveChanges();

            ApplyPostSaveChangeLifetime();

            return saveResult;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            int saveResult;

            ApplyPreSaveChangeLifetime();

            saveResult = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            ApplyPostSaveChangeLifetime();

            return saveResult;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            int saveResult;

            ApplyPreSaveChangeLifetime();

            saveResult = await base.SaveChangesAsync(cancellationToken);

            ApplyPostSaveChangeLifetime();

            return saveResult;
        }

        protected virtual void ApplyPreSaveChangeLifetime()
        {
            if (_repositoryLifetimes.Count == 0)
                return;

            ChangeTracker.Entries().ToList().ForEach(entity =>
            {
                _repositoryLifetimes.ForEach(repositoryLifetime =>
                {
                    repositoryLifetime.PreSaveChanges(ConvertDbContextEntityState(entity.State), entity.Entity);
                });
            });
        }

        protected virtual void ApplyPostSaveChangeLifetime()
        {
            if (_repositoryLifetimes.Count == 0)
                return;

            ChangeTracker.Entries().ToList().ForEach(entity =>
            {
                _repositoryLifetimes.ForEach(repositoryLifetime =>
                {
                    repositoryLifetime.PostSaveChanges(ConvertDbContextEntityState(entity.State), entity.Entity);
                });
            });
        }

        protected virtual RepositoryEntityState ConvertDbContextEntityState(EntityState dbcontextEntityState)
        {
            return dbcontextEntityState switch
            {
                EntityState.Unchanged => RepositoryEntityState.Unchanged,
                EntityState.Deleted => RepositoryEntityState.Deleted,
                EntityState.Modified => RepositoryEntityState.Modified,
                EntityState.Added => RepositoryEntityState.Added,
                _ => RepositoryEntityState.Unchanged
            };
        }
    }
}
