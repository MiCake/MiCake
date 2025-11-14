using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
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

        private readonly AsyncLocal<CacheContext> _asyncLocalCache = new();
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private class CacheContext
        {
            public Guid UowId { get; set; }
            public TDbContext DbContext { get; set; }
            public DbSet<TEntity> DbSet { get; set; }
            public IQueryable<TEntity> Entities { get; set; }
            public IQueryable<TEntity> EntitiesNoTracking { get; set; }
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
        protected IQueryable<TEntity> Entities => GetOrCreateCacheContext().Entities;

        /// <summary>
        /// Gets an IQueryable for the entity with change tracking disabled.
        /// Use this for read-only queries to improve performance.
        /// </summary>
        protected IQueryable<TEntity> EntitiesNoTracking => GetOrCreateCacheContext().EntitiesNoTracking;

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
        /// This method ensures that DbContext and DbSet instances are reused within the same UoW scope.
        /// If no active Unit of Work exists, falls back to directly injected DbContext from DI container.
        /// </summary>
        private CacheContext GetOrCreateCacheContext()
        {
            var currentUow = Dependencies.UnitOfWorkManager.Current;

            if (currentUow == null)
            {
                // Fallback to DI DbContext when no UoW is active
                // This allows repository usage without forcing UoW requirement
                Logger.LogDebug("No active Unit of Work, using directly injected DbContext for {RepositoryType}",
                    typeof(EFRepositoryBase<TDbContext, TEntity, TKey>).Name);

                try
                {
                    TDbContext dbContext = Dependencies.ContextFactory.GetDbContext();
                    
                    if (dbContext == null)
                    {
                        throw new InvalidOperationException(
                            $"DbContext of type {typeof(TDbContext).Name} is not registered in the service container. " +
                            $"Please either: (1) wrap your operation in a Unit of Work using 'using var uow = unitOfWorkManager.BeginAsync();' " +
                            $"or (2) ensure your DbContext is properly registered with the service provider using AddDbContext or AddDbContextPool.");
                    }

                    return new CacheContext
                    {
                        UowId = Guid.Empty,  // Special marker for non-UoW access
                        DbContext = dbContext,
                        DbSet = dbContext.Set<TEntity>(),
                        Entities = dbContext.Set<TEntity>().AsQueryable(),
                        EntitiesNoTracking = dbContext.Set<TEntity>().AsNoTracking()
                    };
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to access DbContext for {typeof(TDbContext).Name} outside of a Unit of Work scope. " +
                        $"Please either: (1) wrap your operation in a Unit of Work using 'using var uow = unitOfWorkManager.BeginAsync();' " +
                        $"or (2) ensure your DbContext is properly registered with the service provider using AddDbContext or AddDbContextPool.",
                        ex);
                }
            }

            var cacheKey = currentUow.Id;

            var cached = _asyncLocalCache.Value;
            if (cached != null && cached.UowId == cacheKey)
                return cached;

            _initLock.Wait();
            try
            {
                // Double-check after acquiring lock
                cached = _asyncLocalCache.Value;
                if (cached != null && cached.UowId == cacheKey)
                    return cached;

                cached = CreateCacheContext(cacheKey);
                _asyncLocalCache.Value = cached;
                return cached;
            }
            finally
            {
                _initLock.Release();
            }
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
            cache.Entities = cache.DbSet.AsQueryable();
            cache.EntitiesNoTracking = cache.DbSet.AsNoTracking();

            return cache;
        }

        #endregion
    }
}
