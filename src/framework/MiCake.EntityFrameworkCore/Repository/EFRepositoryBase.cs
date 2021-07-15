using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

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
        /// Use to get need services.For example:DbContextProvider,POManager,etcs.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Current DbContext.
        /// </summary>
        protected virtual TDbContext DbContext => _currentDbContext;

        /// <summary>
        /// The DbSet for current aggregate root.
        /// </summary>
        protected virtual DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

        private TDbContext _currentDbContext;
        private IDbContextProvider<TDbContext> _dbContextProvider;

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
            _dbContextProvider = ServiceProvider.GetService<IDbContextProvider<TDbContext>>() ??
                throw new ArgumentNullException($"Cannot get {nameof(IDbContextProvider)},current repository initialization failed.");

            _currentDbContext = _dbContextProvider.GetDbContext();
        }
    }
}
