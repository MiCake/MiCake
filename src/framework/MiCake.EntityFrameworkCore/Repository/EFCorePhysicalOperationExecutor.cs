using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Scoped implementation of <see cref="IEFCorePhysicalOperationExecutor{TDbContext}"/>.
    /// Guards the write against missing/read-only units of work, resolves the frame-stable
    /// context, and delegates to EF Core <c>ExecuteDeleteAsync</c>, which the MiCake command
    /// interceptor binds to the unit of work transaction.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type</typeparam>
    public class EFCorePhysicalOperationExecutor<TDbContext> : IEFCorePhysicalOperationExecutor<TDbContext>
        where TDbContext : DbContext
    {
        private readonly IEFCoreContextFactory<TDbContext> _contextFactory;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// Creates a new physical-operation executor.
        /// </summary>
        /// <param name="contextFactory">The frame-stable context factory</param>
        /// <param name="unitOfWorkManager">The unit of work manager</param>
        /// <exception cref="ArgumentNullException">Thrown when a dependency is null</exception>
        public EFCorePhysicalOperationExecutor(
            IEFCoreContextFactory<TDbContext> contextFactory,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _unitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteDeleteAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(predicate);

            var current = _unitOfWorkManager.Current
                ?? throw new InvalidOperationException(
                    $"Physical delete on {typeof(TDbContext).Name} requires an active writable unit of work. " +
                    "Begin one with IUnitOfWorkManager.BeginAsync() before executing physical operations.");

            if (current.IsReadOnly)
            {
                throw new InvalidOperationException(
                    $"Physical delete on {typeof(TDbContext).Name} is rejected: the active unit of work {current.Id} is read-only.");
            }

            // Activate the unit of work transaction explicitly before the delete so the
            // operation is rollback-safe even when this context has no command interceptor
            // installed. When the interceptor is present it binds the resulting command to
            // the same transaction.
            var context = _contextFactory.GetDbContext();
            var wrapper = _contextFactory.GetOrCreateWrapperFor(context);
            await wrapper.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);

            return await wrapper.DbContext.Set<TEntity>()
                .Where(predicate)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
