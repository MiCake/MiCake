using MiCake.DDD.Extensions.Lifetime;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// This interceptor is designed to execute the <see cref="IRepositoryLifetime"/> events.
    /// </summary>
    internal class MiCakeEFCoreInterceptor : ISaveChangesInterceptor
    {
        private readonly IServiceProvider _micakeRootServices;
        private readonly IEFSaveChangesLifetime _saveChangesLifetime;

        private IEnumerable<EntityEntry> _efcoreEntries;

        public MiCakeEFCoreInterceptor(IServiceProvider services)
        {
            _micakeRootServices = services ?? throw new ArgumentException($"{nameof(MiCakeEFCoreInterceptor)} received a null value of {nameof(IServiceProvider)}");

            var currentScoped = _micakeRootServices.CreateScope();      // notice:current scoped will not release,beacuse efcore interceptor use current class instance.
            _saveChangesLifetime = currentScoped.ServiceProvider.GetService<IEFSaveChangesLifetime>() ??
                                    throw new ArgumentNullException($"Can not reslove {nameof(IEFSaveChangesLifetime)},Please check has added UseEFCore() in MiCake.");
        }

        public void SaveChangesFailed(DbContextErrorEventData eventData)
        {
        }

        public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            // do nothing.
            return Task.CompletedTask;
        }

        public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            _saveChangesLifetime.AfterSaveChanges(_efcoreEntries);
            return result;
        }

        public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await _saveChangesLifetime.AfterSaveChangesAsync(_efcoreEntries);
            return result;
        }

        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            _saveChangesLifetime.BeforeSaveChanges(GetChangeEntities(eventData.Context));
            return result;
        }

        public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await _saveChangesLifetime.BeforeSaveChangesAsync(GetChangeEntities(eventData.Context), cancellationToken);
            return result;
        }

        private IEnumerable<EntityEntry> GetChangeEntities(DbContext dbContext)
        {
            return _efcoreEntries = dbContext.ChangeTracker.Entries();   // ChangeTracker.Entries() and ChangeTracker.Entries<TEntity>() will trigger ChangeTracker.DetectChanges();
        }
    }
}
