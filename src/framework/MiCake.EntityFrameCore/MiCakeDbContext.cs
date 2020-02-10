using JetBrains.Annotations;
using MiCake.DDD.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        protected List<IEfRepositoryPreSaveChanges> PreLifetimeInstance { get; private set; }
        protected List<IEfRepositoryPostSaveChanges> PostLifetimeInstance { get; private set; }
        protected IServiceProvider ServiceProvider { get; }

        protected MiCakeDbContext()
        {
        }

        public MiCakeDbContext(
            [NotNull] DbContextOptions options,
            IServiceProvider serviceProvider) : base(options)
        {
            ServiceProvider = serviceProvider;

            PreLifetimeInstance = serviceProvider.GetServices<IEfRepositoryPreSaveChanges>().ToList();
            PostLifetimeInstance = serviceProvider.GetServices<IEfRepositoryPostSaveChanges>().ToList();
        }

        public override int SaveChanges()
        {
            int saveResult;
            var trackerEntities = ChangeTracker.Entries().ToList();

            ApplyRepositoryPreLifetime(trackerEntities);

            saveResult = base.SaveChanges();

            ApplyRepositoryPostLifetime(trackerEntities);

            return saveResult;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            int saveResult;
            var trackerEntities = ChangeTracker.Entries().ToList();

            await ApplyRepositoryPreLifetimeAsync(trackerEntities, cancellationToken);

            saveResult = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            await ApplyRepositoryPostLifetimeAsync(trackerEntities, cancellationToken);

            return saveResult;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            int saveResult;
            var trackerEntities = ChangeTracker.Entries().ToList();

            await ApplyRepositoryPreLifetimeAsync(trackerEntities,cancellationToken);

            saveResult = await base.SaveChangesAsync(cancellationToken);

            await ApplyRepositoryPostLifetimeAsync(trackerEntities, cancellationToken);

            return saveResult;
        }

        protected virtual void ApplyRepositoryPreLifetime(List<EntityEntry> trackerEntities)
        {
            if (PreLifetimeInstance.Count == 0)
                return;

            PreLifetimeInstance.ForEach(repositoryLifetime =>
            {
                trackerEntities.ForEach(entity =>
                {
                    repositoryLifetime.PreSaveChanges(ConvertDbContextEntityState(entity.State), entity.Entity);
                });
            });
        }

        protected virtual void ApplyRepositoryPostLifetime(List<EntityEntry> trackerEntities)
        {
            if (PostLifetimeInstance.Count == 0)
                return;

            PostLifetimeInstance.ForEach(repositoryLifetime =>
            {
                trackerEntities.ForEach(entity =>
                {
                    repositoryLifetime.PostSaveChanges(ConvertDbContextEntityState(entity.State), entity.Entity);
                });
            });
        }

        protected virtual async Task ApplyRepositoryPreLifetimeAsync(
            List<EntityEntry> trackerEntities,
            CancellationToken cancellationToken)
        {
            if (PreLifetimeInstance.Count == 0)
                return;

            foreach (var repositoryLifetime in PreLifetimeInstance)
            {
                foreach (var entity in trackerEntities)
                {
                    await repositoryLifetime.PreSaveChangesAsync(
                        ConvertDbContextEntityState(entity.State),
                        entity.Entity,
                        cancellationToken);
                }
            }
        }

        protected virtual async Task ApplyRepositoryPostLifetimeAsync(
            List<EntityEntry> trackerEntities,
            CancellationToken cancellationToken)
        {
            if (PostLifetimeInstance.Count == 0)
                return;

            foreach (var repositoryLifetime in PostLifetimeInstance)
            {
                foreach (var entity in trackerEntities)
                {
                    await repositoryLifetime.PostSaveChangesAsync(
                        ConvertDbContextEntityState(entity.State),
                        entity.Entity,
                        cancellationToken);
                }
            }
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
