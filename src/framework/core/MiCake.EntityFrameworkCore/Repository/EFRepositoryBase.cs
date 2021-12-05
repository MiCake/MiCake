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

        protected virtual async Task<DbSet<TEntity>> GetDbSetAsync(CancellationToken cancellationToken = default)
        {
            return (await GetDbContextAsync(cancellationToken)).Set<TEntity>();
        }
    }
}
