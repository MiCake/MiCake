using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Base repository class for Entity Framework Core implementations.
    /// Provides common functionality for accessing and manipulating entities through EF Core DbContext.
    /// This class follows the MiCake framework's dependency wrapper pattern for clean dependency injection.
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TEntity, TKey>
            where TEntity : class, IEntity<TKey>
            where TDbContext : DbContext
            where TKey : notnull
    {
        /// <summary>
        /// Gets the dependency wrapper containing all required services
        /// </summary>
        protected readonly EFRepositoryDependencies<TDbContext> Dependencies;

        /// <summary>
        /// Per-repository instance cache of DbContext/DbSet per Unit of Work ID.
        /// Keyed by UnitOfWorkId to support multiple concurrent or nested UoWs.
        /// </summary>
        private readonly Dictionary<Guid, CacheContext> _contextCache = [];
        
        /// <summary>
        /// Protects the cache dictionary and the subscription set during concurrent access.
        /// Uses ReaderWriterLockSlim for better performance with many readers and few writers.
        /// </summary>
        private readonly ReaderWriterLockSlim _cacheLock = new();
        
        /// <summary>
        /// Tracks which UoW instances we've already subscribed to for cleanup events.
        /// Prevents duplicate event handler registration when accessing same UoW multiple times.
        /// </summary>
        private readonly HashSet<Guid> _subscribedUowIds = new();

        private sealed class CacheContext
        {
            public Guid UowId { get; set; }
            public required TDbContext DbContext { get; set; }
            public required DbSet<TEntity> DbSet { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the repository base class.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        protected EFRepositoryBase(EFRepositoryDependencies<TDbContext> dependencies)
        {
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        /// <summary>
        /// Gets the DbContext instance for the current Unit of Work scope.
        /// The DbContext is cached per Unit of Work to ensure consistent state within a transaction.
        /// </summary>
        protected TDbContext DbContext => GetOrCreateCacheContext().DbContext;

        /// <summary>
        /// Gets the DbSet for the entity type.
        /// The DbSet is cached per Unit of Work for performance optimization.
        /// </summary>
        protected DbSet<TEntity> DbSet => GetOrCreateCacheContext().DbSet;

        /// <summary>
        /// Gets an IQueryable for the entity with change tracking enabled.
        /// Use this when you need to track changes to entities for updates.
        /// </summary>
        protected IQueryable<TEntity> Entities => GetOrCreateCacheContext().DbSet.AsQueryable();

        /// <summary>
        /// Gets an IQueryable for the entity with change tracking disabled.
        /// Use this for read-only queries to improve performance.
        /// </summary>
        protected IQueryable<TEntity> EntitiesNoTracking => GetOrCreateCacheContext().DbSet.AsNoTracking();

        /// <summary>
        /// Gets the logger instance for diagnostic logging
        /// </summary>
        protected ILogger Logger => Dependencies.Logger;

        /// <summary>
        /// Asynchronously gets the DbContext instance for the current Unit of Work scope.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task that represents the DbContext</returns>
        protected Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Dependencies.ContextFactory.GetDbContext());
        }

        /// <summary>
        /// Asynchronously gets the DbSet for the entity type.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task that represents the DbSet</returns>
        protected Task<DbSet<TEntity>> GetDbSetAsync(CancellationToken cancellationToken = default)
        {
            var context = Dependencies.ContextFactory.GetDbContext();
            return Task.FromResult(context.Set<TEntity>());
        }

        #region UoW-Aware Caching Implementation

        /// <summary>
        /// Gets or creates a cached context for the current Unit of Work.
        /// Uses Dictionary keyed by UoW ID, with read-write lock for thread-safe concurrent access.
        /// Automatically subscribes to UoW cleanup events to remove stale caches.
        /// </summary>
        private CacheContext GetOrCreateCacheContext()
        {
            var currentUow = Dependencies.UnitOfWorkManager.Current;
            
            // If no active UoW, create a temporary cache without storing it
            if (currentUow == null)
            {
                var tempCache = CreateCacheContext(Guid.Empty);
                return tempCache;
            }

            var cacheKey = currentUow.Id;

            _cacheLock.EnterReadLock();
            try
            {
                if (_contextCache.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            _cacheLock.EnterWriteLock();
            try
            {
                if (_contextCache.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }

                // Create new cache context
                var newCache = CreateCacheContext(cacheKey);
                _contextCache[cacheKey] = newCache;

                // Subscribe to cleanup events (only once per UoW instance)
                // Check if we haven't already subscribed to this specific UoW
                if (!_subscribedUowIds.Contains(cacheKey))
                {
                    SubscribeToUowCleanup(currentUow, cacheKey);
                    _subscribedUowIds.Add(cacheKey);
                }

                return newCache;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Subscribes to UoW commit/rollback events to clean up the cache.
        /// This ensures that after a UoW completes, its cached context is removed,
        /// allowing fresh contexts to be created for the next UoW.
        /// </summary>
        private void SubscribeToUowCleanup(IUnitOfWork uow, Guid uowId)
        {
            // Create a delegate that can be stored for later unsubscription
            EventHandler<UnitOfWorkEventArgs>? cleanupHandler = null;
            
            cleanupHandler = (sender, args) =>
            {
                if (args.UnitOfWorkId == uowId && cleanupHandler != null)
                {
                    _cacheLock.EnterWriteLock();
                    try
                    {
                        _contextCache.Remove(uowId);
                        _subscribedUowIds.Remove(uowId);
                    }
                    finally
                    {
                        _cacheLock.ExitWriteLock();
                    }
                    
                    // Unsubscribe after cleanup to prevent memory leaks
                    try
                    {
                        uow.OnCommitted -= cleanupHandler;
                        uow.OnRolledBack -= cleanupHandler;
                    }
                    catch
                    {
                        // Ignore errors during unsubscribe (may happen if uow is already disposed)
                    }
                }
            };

            // Subscribe to both commit and rollback events
            uow.OnCommitted += cleanupHandler;
            uow.OnRolledBack += cleanupHandler;
        }

        /// <summary>
        /// Creates a new cache context containing DbContext and DbSet instances.
        /// </summary>
        private CacheContext CreateCacheContext(Guid uowId)
        {
            TDbContext dbContext;
            try
            {
                dbContext = Dependencies.ContextFactory.GetDbContext();

                if (dbContext == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create a DbContext for {typeof(TDbContext).Name}. " +
                        "Please ensure you've registered your DbContext with the service provider using AddDbContext or AddDbContextPool.");
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create a DbContext for {typeof(TDbContext).Name}. " +
                    "Please ensure you've registered your DbContext with the service provider using AddDbContext or AddDbContextPool.",
                    ex);
            }

            var cache = new CacheContext
            {
                UowId = uowId,
                DbContext = dbContext,
                DbSet = dbContext.Set<TEntity>(),
            };

            return cache;
        }

        #endregion
    }
}
