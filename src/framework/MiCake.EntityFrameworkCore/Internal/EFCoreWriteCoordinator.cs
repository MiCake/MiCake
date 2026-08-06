using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Classifies a framework-supported write operation for guards and diagnostics.
    /// </summary>
    internal enum EFWriteOperationKind
    {
        SaveChanges,
        ExecuteUpdate,
        ExecuteDelete,
        RawSql,
        PhysicalDelete,
        UnknownNonQuery,
        DatabaseInitialization
    }

    /// <summary>
    /// Guards and activates the explicit unit of work transaction before any supported EF write,
    /// and binds non-query commands to that transaction before SQL execution.
    /// </summary>
    internal interface IEFCoreWriteCoordinator
    {
        void BeforeWrite(DbContext context, EFWriteOperationKind operationKind);

        ValueTask BeforeWriteAsync(
            DbContext context,
            EFWriteOperationKind operationKind,
            CancellationToken cancellationToken = default);

        void BeforeCommand(DbContext context, DbCommand command, EFWriteOperationKind operationKind);

        ValueTask BeforeCommandAsync(
            DbContext context,
            DbCommand command,
            EFWriteOperationKind operationKind,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Scoped implementation of <see cref="IEFCoreWriteCoordinator"/>.
    /// Rejects missing and read-only units of work, resolves the frame-stable resource wrapper,
    /// activates its explicit transaction, and binds the current provider transaction to the
    /// command before EF executes it.
    /// </summary>
    internal sealed class EFCoreWriteCoordinator : IEFCoreWriteCoordinator
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextTypeRegistry _contextTypeRegistry;
        private readonly ILogger<EFCoreWriteCoordinator> _logger;

        public EFCoreWriteCoordinator(
            IUnitOfWorkManager unitOfWorkManager,
            IServiceProvider serviceProvider,
            IDbContextTypeRegistry contextTypeRegistry,
            ILogger<EFCoreWriteCoordinator> logger)
        {
            _unitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _contextTypeRegistry = contextTypeRegistry ?? throw new ArgumentNullException(nameof(contextTypeRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void BeforeWrite(DbContext context, EFWriteOperationKind operationKind)
        {
            // SaveChanges is never an unknown non-query; the coordinator cannot return null here.
            var wrapper = GuardAndResolve(context, operationKind)
                ?? throw new InvalidOperationException(
                    $"Write operation {operationKind} on {context.GetType().Name} cannot be guarded without a unit of work.");
            wrapper.EnsureTransaction();
            _logger.LogDebug(
                "Activated transaction for {DbContextType} before {OperationKind} in UoW {UowId} with resource {ResourceId}",
                context.GetType().Name, operationKind, _unitOfWorkManager.Current?.Id, wrapper.Id);
        }

        public async ValueTask BeforeWriteAsync(
            DbContext context,
            EFWriteOperationKind operationKind,
            CancellationToken cancellationToken = default)
        {
            var wrapper = GuardAndResolve(context, operationKind)
                ?? throw new InvalidOperationException(
                    $"Write operation {operationKind} on {context.GetType().Name} cannot be guarded without a unit of work.");
            await wrapper.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(
                "Activated transaction for {DbContextType} before {OperationKind} in UoW {UowId} with resource {ResourceId}",
                context.GetType().Name, operationKind, _unitOfWorkManager.Current?.Id, wrapper.Id);
        }

        public void BeforeCommand(DbContext context, DbCommand command, EFWriteOperationKind operationKind)
        {
            var wrapper = GuardAndResolve(context, operationKind);
            if (wrapper == null)
            {
                return;
            }

            wrapper.EnsureTransaction();
            BindTransaction(context, command);
            _logger.LogDebug(
                "Bound transaction for {DbContextType} before {OperationKind} in UoW {UowId} with resource {ResourceId}",
                context.GetType().Name, operationKind, _unitOfWorkManager.Current?.Id, wrapper.Id);
        }

        public async ValueTask BeforeCommandAsync(
            DbContext context,
            DbCommand command,
            EFWriteOperationKind operationKind,
            CancellationToken cancellationToken = default)
        {
            var wrapper = GuardAndResolve(context, operationKind);
            if (wrapper == null)
            {
                return;
            }

            await wrapper.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
            BindTransaction(context, command);
            _logger.LogDebug(
                "Bound transaction for {DbContextType} before {OperationKind} in UoW {UowId} with resource {ResourceId}",
                context.GetType().Name, operationKind, _unitOfWorkManager.Current?.Id, wrapper.Id);
        }

        private EFCoreDbContextWrapper? GuardAndResolve(DbContext context, EFWriteOperationKind operationKind)
        {
            ArgumentNullException.ThrowIfNull(context);

            var current = _unitOfWorkManager.Current;
            if (current == null)
            {
                if (operationKind == EFWriteOperationKind.DatabaseInitialization)
                {
                    // Database-initiation DDL (EnsureCreated/Migrate) without an ambient UoW is
                    // not an application write; it passes through unbound.
                    _logger.LogWarning(
                        "Database-initiation command on {DbContextType} executed without an active unit of work; " +
                        "leaving it unbound.",
                        context.GetType().Name);
                    return null;
                }

                throw new InvalidOperationException(
                    $"Write operation {operationKind} on {context.GetType().Name} requires an active writable unit of work. " +
                    "Begin one with IUnitOfWorkManager.BeginAsync() before saving or executing write commands.");
            }

            if (current.IsReadOnly)
            {
                throw new InvalidOperationException(
                    $"Write operation {operationKind} on {context.GetType().Name} is rejected: the active unit of work {current.Id} is read-only.");
            }

            return ResolveWrapper(context);
        }

        private EFCoreDbContextWrapper ResolveWrapper(DbContext context)
        {
            var contextType = context.GetType();
            if (!_contextTypeRegistry.GetRegisteredTypes().Contains(contextType))
            {
                throw new InvalidOperationException(
                    $"DbContext {contextType.Name} is not registered with MiCake. " +
                    "Register it through UseEFCore<TDbContext>() or AddUowCoreServices() so a frame-stable context resource can be resolved.");
            }

            var factoryType = typeof(IEFCoreContextFactory<>).MakeGenericType(contextType);
            var factory = (IEFCoreContextFactory)_serviceProvider.GetRequiredService(factoryType);
            return factory.GetDbContextWrapper();
        }

        private static void BindTransaction(DbContext context, DbCommand command)
        {
            var currentTransaction = context.Database.CurrentTransaction
                ?? throw new InvalidOperationException(
                    $"No active provider transaction is available for {context.GetType().Name}; the write command cannot be bound before execution.");

            var dbTransaction = currentTransaction.GetDbTransaction()
                ?? throw new InvalidOperationException(
                    $"The provider transaction for {context.GetType().Name} has no underlying DbTransaction; the write command cannot be bound.");

            if (command.Transaction != null && !ReferenceEquals(command.Transaction, dbTransaction))
            {
                throw new InvalidOperationException(
                    $"The command on {context.GetType().Name} already has a conflicting transaction assigned; refusing to execute the write outside the unit of work transaction.");
            }

            if (command.Connection != null && !ReferenceEquals(command.Connection, dbTransaction.Connection))
            {
                throw new InvalidOperationException(
                    $"The command connection does not match the active transaction connection on {context.GetType().Name}; refusing to execute the write outside the unit of work transaction.");
            }

            command.Transaction = dbTransaction;
        }
    }
}
