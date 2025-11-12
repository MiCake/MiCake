using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Full-featured repository implementation for Entity Framework Core.
    /// Provides complete CRUD operations for aggregate roots including add, update, delete, and query operations.
    /// This is the primary repository implementation for DDD aggregate roots with EF Core persistence.
    /// </summary>
    /// <typeparam name="TDbContext">The Entity Framework DbContext type</typeparam>
    /// <typeparam name="TAggregateRoot">The aggregate root type that implements IAggregateRoot</typeparam>
    /// <typeparam name="TKey">The primary key type of the aggregate root</typeparam>
    /// <remarks>
    /// This class implements the full Repository pattern with write operations, following DDD principles.
    /// It extends <see cref="EFReadOnlyRepository{TDbContext, TAggregateRoot, TKey}"/> to include modification operations.
    /// Users should inherit from this class to create custom repositories with domain-specific operations.
    /// All write operations participate in the current Unit of Work transaction if one is active.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderRepository : EFRepository&lt;MyDbContext, Order, int&gt;
    /// {
    ///     public OrderRepository(EFRepositoryDependencies&lt;MyDbContext&gt; dependencies) 
    ///         : base(dependencies)
    ///     {
    ///     }
    ///     
    ///     public async Task&lt;Order&gt; CreateOrderAsync(CreateOrderCommand command)
    ///     {
    ///         var order = new Order(command);
    ///         await AddAsync(order);
    ///         await SaveChangesAsync();
    ///         return order;
    ///     }
    /// }
    /// </code>
    /// </example>
    public class EFRepository<TDbContext, TAggregateRoot, TKey> :
        EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the repository.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public EFRepository(EFRepositoryDependencies<TDbContext> dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// Adds a new aggregate root to the repository and optionally saves changes immediately.
        /// Returns the added entity, which is useful for retrieving auto-generated keys.
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to add</param>
        /// <param name="saveNow">
        /// If true, immediately saves changes to the database to retrieve auto-generated keys.
        /// If false, changes are tracked but not persisted until SaveChangesAsync is called.
        /// Default is true.
        /// </param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the added aggregate root with any database-generated values populated.
        /// </returns>
        /// <remarks>
        /// Use this method when you need to retrieve auto-generated values (like identity columns) immediately.
        /// For bulk insertions, consider using AddAsync with saveNow=false and calling SaveChangesAsync once.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when aggregateRoot is null</exception>
        /// <example>
        /// <code>
        /// var order = new Order { CustomerName = "John Doe" };
        /// var savedOrder = await repository.AddAndReturnAsync(order);
        /// // savedOrder.Id now contains the database-generated ID
        /// </code>
        /// </example>
        public virtual async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool saveNow = true, CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entityInfo = await dbcontext.Set<TAggregateRoot>().AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);

            if (saveNow)
                await dbcontext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return entityInfo.Entity;
        }

        /// <summary>
        /// Adds a new aggregate root to the repository.
        /// Changes are tracked but not persisted until SaveChangesAsync is called.
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to add</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <remarks>
        /// This method only marks the entity for insertion. Call SaveChangesAsync to persist changes.
        /// For better performance with multiple entities, add them all before calling SaveChangesAsync once.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when aggregateRoot is null</exception>
        /// <example>
        /// <code>
        /// await repository.AddAsync(order1);
        /// await repository.AddAsync(order2);
        /// await repository.SaveChangesAsync(); // Saves both orders in one transaction
        /// </code>
        /// </example>
        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears all change tracking for the current DbContext.
        /// This detaches all tracked entities and resets the change tracker.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <remarks>
        /// Use this method with caution as it will discard all pending changes.
        /// This is useful for scenarios where you want to start fresh without pending modifications,
        /// such as after handling an error or when switching between read and write operations.
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     await repository.UpdateAsync(order);
        /// }
        /// catch (Exception)
        /// {
        ///     await repository.ClearChangeTrackingAsync(); // Discard changes
        /// }
        /// </code>
        /// </example>
        public async Task ClearChangeTrackingAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbcontext.ChangeTracker.Clear();
        }

        /// <summary>
        /// Marks an aggregate root for deletion.
        /// The entity is removed when SaveChangesAsync is called.
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to delete</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <remarks>
        /// This method only marks the entity for deletion. Call SaveChangesAsync to persist the deletion.
        /// The entity must be tracked by the change tracker for this operation to work.
        /// For deleting by ID without loading the entity, use DeleteByIdAsync instead.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when aggregateRoot is null</exception>
        /// <example>
        /// <code>
        /// var order = await repository.FindAsync(orderId);
        /// if (order != null)
        /// {
        ///     await repository.DeleteAsync(order);
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </example>
        public virtual async Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Remove(aggregateRoot);
        }

        /// <summary>
        /// Deletes an aggregate root by its primary key without loading it first.
        /// This is more efficient than loading the entity and then deleting it.
        /// </summary>
        /// <param name="id">The primary key value of the aggregate root to delete</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <remarks>
        /// This method uses EF Core's ExecuteDeleteAsync for efficient deletion without loading the entity.
        /// The deletion is executed immediately and does not require calling SaveChangesAsync.
        /// Use this for better performance when you only have the ID and don't need the entity.
        /// </remarks>
        /// <example>
        /// <code>
        /// await repository.DeleteByIdAsync(orderId);
        /// // Entity is deleted immediately without loading it
        /// </code>
        /// </example>
        public virtual async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.Where(e => e.Id.Equals(id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves all pending changes to the database.
        /// This includes all tracked additions, modifications, and deletions.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the number of state entries written to the database.
        /// </returns>
        /// <remarks>
        /// This method commits all pending changes within the current Unit of Work transaction.
        /// Domain events from aggregate roots are automatically dispatched during this operation.
        /// If there's an active Unit of Work, the transaction is managed by the UoW coordinator.
        /// </remarks>
        /// <example>
        /// <code>
        /// await repository.AddAsync(order1);
        /// await repository.UpdateAsync(order2);
        /// var affectedRows = await repository.SaveChangesAsync();
        /// Console.WriteLine($"{affectedRows} rows affected");
        /// </code>
        /// </example>
        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await dbcontext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks an aggregate root for update.
        /// Changes to the entity are tracked and persisted when SaveChangesAsync is called.
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to update</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <remarks>
        /// This method explicitly marks the entity as modified. Call SaveChangesAsync to persist changes.
        /// If the entity is already being tracked with modifications, this method ensures it's marked for update.
        /// For better performance, you can modify tracked entities directly without calling this method.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when aggregateRoot is null</exception>
        /// <example>
        /// <code>
        /// var order = await repository.FindAsync(orderId);
        /// order.UpdateStatus(OrderStatus.Shipped);
        /// await repository.UpdateAsync(order);
        /// await repository.SaveChangesAsync();
        /// </code>
        /// </example>
        public virtual async Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Update(aggregateRoot);
        }
    }
}
