using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Base repository for EFCore.
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TEntity, TKey>
            where TEntity : class, IEntity<TKey>
            where TDbContext : DbContext
    {
        private readonly IEFCoreContextFactory<TDbContext> _contextFactory;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly MiCakeEFCoreOptions _efCoreOptions;
        protected readonly ILogger Logger;

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

        protected EFRepositoryBase(IServiceProvider serviceProvider)
        {
            _contextFactory = serviceProvider.GetRequiredService<IEFCoreContextFactory<TDbContext>>();
            _unitOfWorkManager = serviceProvider.GetRequiredService<IUnitOfWorkManager>();
            _efCoreOptions = serviceProvider.GetRequiredService<IObjectAccessor<MiCakeEFCoreOptions>>().Value;
            Logger = serviceProvider.GetRequiredService<ILogger<EFRepositoryBase<TDbContext, TEntity, TKey>>>();
        }

        /// <summary>
        /// Gets the DbContext
        /// </summary>
        protected TDbContext DbContext => GetOrCreateCacheContext().DbContext;

        /// <summary>
        /// Gets the DbSet
        /// </summary>
        protected DbSet<TEntity> DbSet => GetOrCreateCacheContext().DbSet;

        /// <summary>
        /// Gets entities with tracking
        /// </summary>
        protected IQueryable<TEntity> Entities => GetOrCreateCacheContext().Entities;

        /// <summary>
        /// Gets entities without tracking
        /// </summary>
        protected IQueryable<TEntity> EntitiesNoTracking => GetOrCreateCacheContext().EntitiesNoTracking;

        /// <summary>
        /// Async version for getting DbContext
        /// </summary>
        protected Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_contextFactory.GetDbContext());
        }

        /// <summary>
        /// Async version for getting DbSet
        /// </summary>
        protected Task<DbSet<TEntity>> GetDbSetAsync(CancellationToken cancellationToken = default)
        {
            var context = _contextFactory.GetDbContext();
            return Task.FromResult(context.Set<TEntity>());
        }

        #region UoW-Aware Caching Implementation

        private CacheContext GetOrCreateCacheContext()
        {
            var currentUow = _unitOfWorkManager.Current;
            var isUsingImplicitMode = _efCoreOptions.ImplicitModeForUow;

            if (currentUow == null && !isUsingImplicitMode)
            {
                throw new InvalidOperationException(
                    $"Cannot access {typeof(TDbContext).Name} outside of a Unit of Work scope. " +
                    $"Please wrap your operation in: using var uow = unitOfWorkManager.Begin(); " +
                    $"Or enable ImplicitModeForUow in MiCakeEFCoreOptions.");
            }

            var cacheKey = currentUow?.Id ?? Guid.Empty;

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

        private CacheContext CreateCacheContext(Guid uowId)
        {
            TDbContext dbContext;
            try
            {
                dbContext = _contextFactory.GetDbContext();
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
