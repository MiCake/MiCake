using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Read-only repository implementation for Entity Framework Core.
    /// Provides query and retrieval operations for aggregate roots without modification capabilities.
    /// This is useful for read-only scenarios like reporting, queries, and data display.
    /// </summary>
    /// <typeparam name="TDbContext">The Entity Framework DbContext type</typeparam>
    /// <typeparam name="TAggregateRoot">The aggregate root type that implements IAggregateRoot</typeparam>
    /// <typeparam name="TKey">The primary key type of the aggregate root</typeparam>
    /// <remarks>
    /// This class implements the Repository pattern for read-only operations, following DDD principles.
    /// For write operations (add, update, delete), use <see cref="EFRepository{TDbContext, TAggregateRoot, TKey}"/> instead.
    /// Users can inherit from this class to create custom read-only repositories with additional query methods.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderReadOnlyRepository : EFReadOnlyRepository&lt;MyDbContext, Order, int&gt;
    /// {
    ///     public OrderReadOnlyRepository(EFRepositoryDependencies&lt;MyDbContext&gt; dependencies) 
    ///         : base(dependencies)
    ///     {
    ///     }
    ///     
    ///     public async Task&lt;List&lt;Order&gt;&gt; GetRecentOrdersAsync(int count)
    ///     {
    ///         return await Query()
    ///             .OrderByDescending(o => o.CreatedDate)
    ///             .Take(count)
    ///             .ToListAsync();
    ///     }
    /// }
    /// </code>
    /// </example>
    public class EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey> :
        EFRepositoryBase<TDbContext, TAggregateRoot, TKey>,
        IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the read-only repository.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public EFReadOnlyRepository(EFRepositoryDependencies<TDbContext> dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// Returns an IQueryable for building complex LINQ queries against the aggregate root.
        /// This allows users to leverage the full power of LINQ for flexible querying.
        /// </summary>
        /// <returns>An IQueryable of the aggregate root with change tracking enabled</returns>
        /// <remarks>
        /// The returned IQueryable has change tracking enabled. For read-only scenarios with better performance,
        /// use the protected <c>EntitiesNoTracking</c> property in derived classes.
        /// </remarks>
        /// <example>
        /// <code>
        /// var orders = await repository.Query()
        ///     .Where(o => o.Status == OrderStatus.Completed)
        ///     .OrderByDescending(o => o.CreatedDate)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public virtual IQueryable<TAggregateRoot> Query()
        {
            return Entities;
        }

        /// <summary>
        /// Finds an aggregate root by its primary key.
        /// Returns null if the entity is not found.
        /// </summary>
        /// <param name="id">The primary key value of the aggregate root to find</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the aggregate root if found; otherwise, null.
        /// </returns>
        /// <remarks>
        /// This method uses EF Core's FindAsync which first checks the change tracker before querying the database.
        /// If the entity is already being tracked, it returns the tracked instance without a database query.
        /// </remarks>
        /// <example>
        /// <code>
        /// var order = await repository.FindAsync(orderId);
        /// if (order != null)
        /// {
        ///     // Process the order
        /// }
        /// </code>
        /// </example>
        public virtual async Task<TAggregateRoot?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            return await dbset.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the total count of all aggregate roots in the repository.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the total number of aggregate roots.
        /// </returns>
        /// <remarks>
        /// This method executes a COUNT query against the database.
        /// For large tables, consider using pagination or filtered queries instead of loading all data.
        /// </remarks>
        /// <example>
        /// <code>
        /// var totalOrders = await repository.GetCountAsync();
        /// Console.WriteLine($"Total orders: {totalOrders}");
        /// </code>
        /// </example>
        public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            return await dbset.LongCountAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
