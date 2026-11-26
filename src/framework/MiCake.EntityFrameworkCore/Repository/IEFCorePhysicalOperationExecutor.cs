using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Executes explicit physical deletion that bypasses aggregate lifecycle processing
    /// (loading, audit mutation, soft deletion, and domain events) while remaining inside
    /// the active unit of work transaction.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type</typeparam>
    /// <remarks>
    /// Physical deletion requires an ambient writable unit of work. It uses the current
    /// frame-stable context, so the delete participates in the unit of work transaction and
    /// rolls back with it. Because lifecycle behavior is bypassed, this API is named and
    /// documented as physical; prefer tracked <c>DeleteAsync</c>/<c>DeleteByIdAsync</c> for
    /// normal aggregate deletion. Executed rows are deleted directly in the database: the
    /// ChangeTracker is not updated, so entities already tracked by the context remain stale
    /// until refreshed or detached.
    /// </remarks>
    public interface IEFCorePhysicalOperationExecutor<TDbContext>
        where TDbContext : DbContext
    {
        /// <summary>
        /// Physically deletes entities matching <paramref name="predicate"/> immediately,
        /// inside the active unit of work transaction and without aggregate lifecycle
        /// processing.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete</typeparam>
        /// <param name="predicate">The predicate selecting the rows to delete</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The number of rows deleted</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no ambient writable unit of work is active.
        /// </exception>
        Task<int> ExecuteDeleteAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
            where TEntity : class;
    }
}
