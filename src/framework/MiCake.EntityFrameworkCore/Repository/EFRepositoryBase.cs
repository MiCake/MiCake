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
    /// Context identity is owned by <see cref="EFRepositoryDependencies{TDbContext}.ContextFactory"/>,
    /// which is frame-stable: the same unit of work and DbContext type always resolve the same
    /// context instance, so repositories hold no per-UoW context cache of their own.
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
        /// Initializes a new instance of the repository base class.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        protected EFRepositoryBase(EFRepositoryDependencies<TDbContext> dependencies)
        {
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        /// <summary>
        /// Gets the DbContext instance for the current unit of work scope.
        /// The frame-stable context factory guarantees the same unit of work and DbContext
        /// type resolve the same context instance.
        /// </summary>
        protected TDbContext DbContext => Dependencies.ContextFactory.GetDbContext();

        /// <summary>
        /// Gets the DbSet for the entity type.
        /// </summary>
        protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

        /// <summary>
        /// Gets an IQueryable for the entity with change tracking enabled.
        /// Use this when you need to track changes to entities for updates.
        /// </summary>
        protected IQueryable<TEntity> Entities => DbSet.AsQueryable();

        /// <summary>
        /// Gets an IQueryable for the entity with change tracking disabled.
        /// Use this for read-only queries to improve performance.
        /// </summary>
        protected IQueryable<TEntity> EntitiesNoTracking => DbSet.AsNoTracking();

        /// <summary>
        /// Gets the logger instance for diagnostic logging
        /// </summary>
        protected ILogger Logger => Dependencies.Logger;

        /// <summary>
        /// Asynchronously gets the DbContext instance for the current unit of work scope.
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
            return Task.FromResult(DbContext.Set<TEntity>());
        }
    }
}
