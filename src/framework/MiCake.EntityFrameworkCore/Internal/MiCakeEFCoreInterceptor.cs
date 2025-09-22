using MiCake.DDD.Extensions.Lifetime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// This interceptor is designed to execute the <see cref="IRepositoryLifetime"/> events.
    /// </summary>
    internal class MiCakeEFCoreInterceptor : ISaveChangesInterceptor
    {
        private readonly IServiceProvider _services;
        private readonly IEFSaveChangesLifetime? _saveChangesLifetime;

        private IEnumerable<EntityEntry> _efcoreEntries;

        public MiCakeEFCoreInterceptor(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentException($"{nameof(MiCakeEFCoreInterceptor)} received a null value of {nameof(IServiceProvider)}");
            _saveChangesLifetime = _services.GetService<IEFSaveChangesLifetime>();
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
            //be careful ,this will risks a deadlock.
            //when save data in aspnet core ,shuold use DbContext.SaveChangesAsync().
            SavedChangesAsync(eventData, result).GetAwaiter().GetResult();
            return result;
        }

        public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (_saveChangesLifetime != null && _efcoreEntries != null)
            {
                await _saveChangesLifetime.AfterSaveChangesAsync(_efcoreEntries, cancellationToken);
            }
            return result;
        }

        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            //be careful ,this will risks a deadlock.
            //when save data in aspnet core ,shuold use DbContext.SaveChangesAsync().
            SavingChangesAsync(eventData, result).GetAwaiter().GetResult();
            return result;
        }

        public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null && _saveChangesLifetime != null)
            {
                await _saveChangesLifetime.BeforeSaveChangesAsync(GetChangeEntities(eventData.Context), cancellationToken);
            }
            return result;
        }

        private IEnumerable<EntityEntry> GetChangeEntities(DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
                
            return _efcoreEntries = dbContext.ChangeTracker.Entries();   // ChangeTracker.Entries() and ChangeTracker.Entries<TEntity>() will trigger ChangeTracker.DetectChanges();
        }
    }
}
