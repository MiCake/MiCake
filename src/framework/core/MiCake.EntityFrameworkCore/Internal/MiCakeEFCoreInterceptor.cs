using MiCake.Cord.Lifetime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// This interceptor is designed to execute the <see cref="IRepositoryLifetime"/> events.
    /// </summary>
    [Obsolete]
    internal class MiCakeEFCoreInterceptor : ISaveChangesInterceptor
    {
        private readonly IServiceProvider _services;
        private readonly IEFSaveChangesLifetime _saveChangesLifetime;

        private IEnumerable<EntityEntry> _efcoreEntries = new List<EntityEntry>();

        public MiCakeEFCoreInterceptor(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentException($"{nameof(MiCakeEFCoreInterceptor)} received a null value of {nameof(IServiceProvider)}");
            _saveChangesLifetime = _services.GetService<IEFSaveChangesLifetime>() ??
                                    throw new InvalidOperationException($"Can not reslove {nameof(IEFSaveChangesLifetime)},Please check has added UseEFCore() in MiCake.");
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
            await _saveChangesLifetime.AfterSaveChangesAsync(_efcoreEntries);
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
            await _saveChangesLifetime.BeforeSaveChangesAsync(GetChangeEntities(eventData.Context!), cancellationToken);
            return result;
        }

        private IEnumerable<EntityEntry> GetChangeEntities(DbContext dbContext)
        {
            return _efcoreEntries = dbContext.ChangeTracker.Entries();   // ChangeTracker.Entries() and ChangeTracker.Entries<TEntity>() will trigger ChangeTracker.DetectChanges();
        }
    }
}
