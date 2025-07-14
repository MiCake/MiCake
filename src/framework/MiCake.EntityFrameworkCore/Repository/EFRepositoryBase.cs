using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// a base repository for EFCore
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TEntity, TKey>
         where TEntity : class, IEntity<TKey>
         where TDbContext : DbContext
    {
        /// <summary>
        /// Use to get need services.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }
        protected IDbContextProvider<TDbContext> DbContextProvider;

        private TDbContext _currentDbContext;
        private DbSet<TEntity> _dbSet;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public EFRepositoryBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            InitComponents();
        }

        /// <summary>
        /// Can use this method to initialization services or other action.
        /// <para>
        ///     for example:can get some di services from <see cref="ServiceProvider"/>
        /// </para>
        /// </summary>
        protected virtual void InitComponents()
        {
            DbContextProvider = ServiceProvider.GetService<IDbContextProvider<TDbContext>>() ??
                throw new ArgumentNullException($"Cannot get {nameof(IDbContextProvider)},current repository initialization failed.");
        }

        protected virtual async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (_currentDbContext == null)
            {
                _currentDbContext = await DbContextProvider.GetDbContextAsync(cancellationToken);
            }

            return _currentDbContext;
        }

        protected virtual DbSet<TEntity> DbSet
        {
            get
            {
                if (_dbSet == null)
                {
                    // Try to get it synchronously first
                    if (_currentDbContext != null)
                    {
                        _dbSet = _currentDbContext.Set<TEntity>();
                    }
                    else
                    {
                        _dbSet = GetDbSetAsync().GetAwaiter().GetResult();
                    }
                }
                return _dbSet;
            }
        }

        protected virtual async Task<DbSet<TEntity>> GetDbSetAsync(CancellationToken cancellationToken = default)
        {
            if (_dbSet == null)
            {
                var context = await GetDbContextAsync(cancellationToken);
                _dbSet = context.Set<TEntity>();
            }
            return _dbSet;
        }
    }
}
