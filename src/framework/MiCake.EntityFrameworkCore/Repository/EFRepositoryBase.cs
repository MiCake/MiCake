using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// a base repository for efcore
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TEntity, TKey>
         where TEntity : class, IEntity<TKey>
         where TDbContext : DbContext
    {
        /// <summary>
        /// Use to get need services.For example:DbContextProvider,POManager,etcs.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Current DbContext.
        /// </summary>
        protected virtual TDbContext DbContext
        {
            get
            {
                if (_currentDbContext != null)
                    return _currentDbContext;

                _currentDbContext = _dbContextProvider.GetDbContext();
                return _currentDbContext;
            }
        }

        /// <summary>
        /// The DbSet for current aggregate root.
        /// </summary>
        protected virtual DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

        private TDbContext _currentDbContext;
        private IDbContextProvider<TDbContext> _dbContextProvider;

        public EFRepositoryBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            InitComponents();
        }

        protected virtual void InitComponents()
        {
            _dbContextProvider = ServiceProvider.GetService<IDbContextProvider<TDbContext>>() ??
                throw new ArgumentNullException($"Cannot get {nameof(IDbContextProvider)},current repository initialization failed.");
        }
    }
}
