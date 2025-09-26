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
    /// a base repository for EFCore with UoW-aware caching
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TEntity, TKey>
            where TEntity : class, IEntity<TKey>
            where TDbContext : DbContext
    {
        private readonly IEFCoreContextFactory<TDbContext> _contextFactory;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly MiCakeEFCoreOptions _efCoreOptions;
        protected readonly ILogger Logger;

        // UoW-aware caching: cache per UoW to avoid cross-UoW contamination
        private readonly Lock _cacheLock = new();
        private Guid? _cachedUowId;
        private TDbContext _cachedDbContext;
        private DbSet<TEntity> _cachedDbSet;
        private IQueryable<TEntity> _cachedEntities;
        private IQueryable<TEntity> _cachedEntitiesNoTracking;

        protected EFRepositoryBase(IServiceProvider serviceProvider)
        {
            _contextFactory = serviceProvider.GetRequiredService<IEFCoreContextFactory<TDbContext>>();
            _unitOfWorkManager = serviceProvider.GetRequiredService<IUnitOfWorkManager>();
            _efCoreOptions = serviceProvider.GetRequiredService<IObjectAccessor<MiCakeEFCoreOptions>>().Value;
            Logger = serviceProvider.GetRequiredService<ILogger<EFRepositoryBase<TDbContext, TEntity, TKey>>>();
        }

        /// <summary>
        /// Gets the DbContext with UoW-aware caching
        /// </summary>
        protected TDbContext DbContext => GetCachedDbContext();

        /// <summary>
        /// Gets the DbSet with UoW-aware caching
        /// </summary>
        protected DbSet<TEntity> DbSet => GetCachedDbSet();

        /// <summary>
        /// Gets entities with tracking
        /// </summary>
        protected IQueryable<TEntity> Entities => GetCachedEntities();

        /// <summary>
        /// Gets entities without tracking
        /// </summary>
        protected IQueryable<TEntity> EntitiesNoTracking => GetCachedEntitiesNoTracking();

        /// <summary>
        /// Async version for getting DbContext
        /// </summary>
        protected async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            return await _contextFactory.GetDbContextAsync(cancellationToken);
        }

        /// <summary>
        /// Async version for getting DbSet
        /// </summary>
        protected async Task<DbSet<TEntity>> GetDbSetAsync(CancellationToken cancellationToken = default)
        {
            var context = await GetDbContextAsync(cancellationToken);
            return context.Set<TEntity>();
        }

        #region UoW-Aware Caching Implementation

        private TDbContext GetCachedDbContext()
        {
            var currentUow = _unitOfWorkManager.Current;
            var isUsingImplicitMode = _efCoreOptions.ImplicitModeForUow;

            // In implicit mode, allow access without UoW; in explicit mode, require UoW
            if (currentUow == null && !isUsingImplicitMode)
            {
                throw new InvalidOperationException(
                    $"Cannot access {typeof(TDbContext).Name} outside of a Unit of Work scope. " +
                    $"Please wrap your operation in: using var uow = unitOfWorkManager.Begin(); " +
                    $"Or enable ImplicitModeForUow in MiCakeEFCoreOptions.");
            }

            lock (_cacheLock)
            {
                var cacheKey = currentUow?.Id ?? Guid.Empty;

                // Check if we need to invalidate cache (different UoW/mode or no cache)
                if (_cachedUowId != cacheKey)
                {
                    InvalidateCache();
                    _cachedUowId = cacheKey;

                    try
                    {
                        _cachedDbContext = _contextFactory.GetDbContextAsync().GetAwaiter().GetResult();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to create a DbContext for {typeof(TDbContext).Name}. " +
                            "Please ensure you've registered your DbContext with the service provider using AddDbContext or AddDbContextPool.",
                            ex);
                    }
                }

                return _cachedDbContext;
            }
        }

        private DbSet<TEntity> GetCachedDbSet()
        {
            lock (_cacheLock)
            {
                _cachedDbSet ??= GetCachedDbContext().Set<TEntity>();
                return _cachedDbSet;
            }
        }

        private IQueryable<TEntity> GetCachedEntities()
        {
            lock (_cacheLock)
            {
                if (_cachedEntities == null)
                {
                    _cachedEntities = GetCachedDbSet().AsQueryable();
                }
                return _cachedEntities;
            }
        }

        private IQueryable<TEntity> GetCachedEntitiesNoTracking()
        {
            lock (_cacheLock)
            {
                _cachedEntitiesNoTracking ??= GetCachedDbSet().AsNoTracking();
                return _cachedEntitiesNoTracking;
            }
        }

        private void InvalidateCache()
        {
            _cachedDbContext = null;
            _cachedDbSet = null;
            _cachedEntities = null;
            _cachedEntitiesNoTracking = null;
        }

        #endregion
    }
}
