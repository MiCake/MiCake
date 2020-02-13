using MiCake.Core.Reactive;
using MiCake.DDD.Extensions;
using MiCake.EntityFrameworkCore.LifeTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Diagnostics
{
    internal class SaveChangesInterceptor : IObserverAsync<KeyValuePair<string, object>>
    {
        private IServiceProvider _serviceProvider;

        public SaveChangesInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                if (value.Key == CoreEventId.SaveChangesStarting.Name)
                {
                    var preLifeTimes = _serviceProvider.GetServices<IEfRepositoryPreSaveChanges>().ToList();

                    if (preLifeTimes.Count != 0)
                    {
                        var dbContext = ((DbContextEventData)value.Value).Context;
                        var trackerEntities = dbContext.ChangeTracker.Entries().ToList();

                        await ApplyRepositoryPreLifetimeAsync(preLifeTimes, trackerEntities);
                    }
                }

                if (value.Key == CoreEventId.SaveChangesCompleted.Name)
                {
                    var postLifeTimes = _serviceProvider.GetServices<IEfRepositoryPostSaveChanges>().ToList();

                    if (postLifeTimes.Count != 0)
                    {
                        var dbContext = ((DbContextEventData)value.Value).Context;
                        var trackerEntities = dbContext.ChangeTracker.Entries().ToList();

                        await ApplyRepositoryPostLifetimeAsync(postLifeTimes, trackerEntities);
                    }
                }
            }
            catch { }
        }

        private async Task ApplyRepositoryPreLifetimeAsync(
            List<IEfRepositoryPreSaveChanges> preLifetimeInstance,
            List<EntityEntry> trackerEntities,
            CancellationToken cancellationToken = default)
        {
            if (preLifetimeInstance.Count == 0)
                return;

            foreach (var repositoryLifetime in preLifetimeInstance)
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

        private async Task ApplyRepositoryPostLifetimeAsync(
            List<IEfRepositoryPostSaveChanges> postLifetimeInstance,
            List<EntityEntry> trackerEntities,
            CancellationToken cancellationToken = default)
        {
            if (postLifetimeInstance.Count == 0)
                return;

            foreach (var repositoryLifetime in postLifetimeInstance)
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

        private RepositoryEntityState ConvertDbContextEntityState(EntityState dbcontextEntityState)
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
