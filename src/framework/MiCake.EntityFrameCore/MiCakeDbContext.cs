using JetBrains.Annotations;
using MiCake.DDD.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore
{
    public class MiCakeDbContext : DbContext
    {
        protected List<IEfRepositoryLifetime> RepositoryLifetimeInstance { get; private set; }
        protected IServiceProvider ServiceProvider { get; }

        protected MiCakeDbContext()
        {
        }

        public MiCakeDbContext(
            [NotNull] DbContextOptions options, 
            IServiceProvider serviceProvider) : base(options)
        {
            ServiceProvider = serviceProvider;
            RepositoryLifetimeInstance = serviceProvider.GetServices<IEfRepositoryLifetime>().ToList();
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
            if (RepositoryLifetimeInstance.Count == 0)
                return;

            ChangeTracker.Entries().ToList().ForEach(entity =>
            {
                RepositoryLifetimeInstance.ForEach(repositoryLifetime =>
                {
                    repositoryLifetime.PreSaveChanges(ConvertDbContextEntityState(entity.State), entity.Entity);
                });
            });
        }

        protected virtual void ApplyPostSaveChangeLifetime()
        {
            if (RepositoryLifetimeInstance.Count == 0)
                return;

            ChangeTracker.Entries().ToList().ForEach(entity =>
            {
                RepositoryLifetimeInstance.ForEach(repositoryLifetime =>
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
