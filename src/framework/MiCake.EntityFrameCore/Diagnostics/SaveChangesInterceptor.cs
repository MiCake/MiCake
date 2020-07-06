using MiCake.Core.Reactive;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Diagnostics
{
    //[Cancel:See Azure Board #ISSUE 12]
    [Obsolete]
    internal class SaveChangesInterceptor : IObserverAsync<KeyValuePair<string, object>>
    {
        //private IServiceProvider _serviceProvider;

        //public SaveChangesInterceptor(IServiceProvider serviceProvider)
        //{
        //    _serviceProvider = serviceProvider;
        //}

        //public async Task OnNext(KeyValuePair<string, object> value)
        //{
        //    try
        //    {
        //        if (value.Key == CoreEventId.SaveChangesStarting.Name)
        //        {
        //            var preLifeTimes = _serviceProvider.GetServices<IRepositoryPreSaveChanges>().ToList();

        //            if (preLifeTimes.Count != 0)
        //            {
        //                var dbContext = ((DbContextEventData)value.Value).Context;
        //                var trackerEntities = dbContext.ChangeTracker.Entries().ToList();

        //                await ApplyRepositoryPreLifetimeAsync(preLifeTimes, trackerEntities);
        //            }
        //        }

        //        if (value.Key == CoreEventId.SaveChangesCompleted.Name)
        //        {
        //            var postLifeTimes = _serviceProvider.GetServices<IRepositoryPostSaveChanges>().ToList();

        //            if (postLifeTimes.Count != 0)
        //            {
        //                var dbContext = ((DbContextEventData)value.Value).Context;
        //                var trackerEntities = dbContext.ChangeTracker.Entries().ToList();

        //                await ApplyRepositoryPostLifetimeAsync(postLifeTimes, trackerEntities);
        //            }
        //        }
        //    }
        //    catch { }
        //}

        //private async Task ApplyRepositoryPreLifetimeAsync(
        //    List<IRepositoryPreSaveChanges> preLifetimeInstance,
        //    List<EntityEntry> trackerEntities,
        //    CancellationToken cancellationToken = default)
        //{
        //    if (preLifetimeInstance.Count == 0)
        //        return;

        //    foreach (var repositoryLifetime in preLifetimeInstance)
        //    {
        //        foreach (var entity in trackerEntities)
        //        {
        //            await repositoryLifetime.PreSaveChangesAsync(
        //                ConvertDbContextEntityState(entity.State),
        //                entity.Entity,
        //                cancellationToken);
        //        }
        //    }
        //}

        //private async Task ApplyRepositoryPostLifetimeAsync(
        //    List<IRepositoryPostSaveChanges> postLifetimeInstance,
        //    List<EntityEntry> trackerEntities,
        //    CancellationToken cancellationToken = default)
        //{
        //    if (postLifetimeInstance.Count == 0)
        //        return;

        //    foreach (var repositoryLifetime in postLifetimeInstance)
        //    {
        //        foreach (var entity in trackerEntities)
        //        {
        //            await repositoryLifetime.PostSaveChangesAsync(
        //                ConvertDbContextEntityState(entity.State),
        //                entity.Entity,
        //                cancellationToken);
        //        }
        //    }
        //}

        //private RepositoryEntityState ConvertDbContextEntityState(EntityState dbcontextEntityState)
        //{
        //    return dbcontextEntityState switch
        //    {
        //        EntityState.Unchanged => RepositoryEntityState.Unchanged,
        //        EntityState.Deleted => RepositoryEntityState.Deleted,
        //        EntityState.Modified => RepositoryEntityState.Modified,
        //        EntityState.Added => RepositoryEntityState.Added,
        //        _ => RepositoryEntityState.Unchanged
        //    };
        //}
        public Task OnNext(KeyValuePair<string, object> value)
        {
            throw new NotImplementedException();
        }
    }
}
