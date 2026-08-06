using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Stateless command interceptor that guards and binds every supported non-query write
    /// command (ExecuteUpdate, ExecuteDelete, ExecuteSqlRaw, and unknown non-query commands
    /// treated conservatively as writes) to the active unit of work transaction.
    /// The coordinator is resolved from the command's DbContext provider; when no MiCake
    /// write pipeline is registered, commands pass through unmodified.
    /// </summary>
    internal sealed class MiCakeDbCommandInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<MiCakeDbCommandInterceptor> _logger;
        private readonly IServiceProvider? _serviceProvider;

        public MiCakeDbCommandInterceptor(
            ILogger<MiCakeDbCommandInterceptor> logger,
            IServiceProvider? serviceProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            var operationKind = Classify(eventData.CommandSource);
            var coordinator = ResolveCoordinator();
            if (coordinator != null)
            {
                if (eventData.Context != null)
                {
                    coordinator.BeforeCommand(eventData.Context, command, operationKind);
                }
            }
            else if (eventData.Context != null && operationKind != EFWriteOperationKind.DatabaseInitialization)
            {
                throw new InvalidOperationException(
                    $"Non-query write command on {eventData.Context.GetType().Name} cannot be guarded because the " +
                    "MiCake write pipeline is not registered for this DbContext. Configure the DbContext with " +
                    "UseMiCakeInterceptors(IServiceProvider) inside AddDbContext and register the MiCake EF Core module.");
            }

            return base.NonQueryExecuting(command, eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var operationKind = Classify(eventData.CommandSource);
            var coordinator = ResolveCoordinator();
            if (coordinator != null)
            {
                if (eventData.Context != null)
                {
                    await coordinator.BeforeCommandAsync(
                            eventData.Context,
                            command,
                            operationKind,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else if (eventData.Context != null && operationKind != EFWriteOperationKind.DatabaseInitialization)
            {
                throw new InvalidOperationException(
                    $"Non-query write command on {eventData.Context.GetType().Name} cannot be guarded because the " +
                    "MiCake write pipeline is not registered for this DbContext. Configure the DbContext with " +
                    "UseMiCakeInterceptors(IServiceProvider) inside AddDbContext and register the MiCake EF Core module.");
            }

            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
        }

        private IEFCoreWriteCoordinator? ResolveCoordinator()
        {
            try
            {
                return (IEFCoreWriteCoordinator?)_serviceProvider?.GetService(typeof(IEFCoreWriteCoordinator));
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        internal static EFWriteOperationKind Classify(CommandSource commandSource)
            => commandSource switch
            {
                CommandSource.SaveChanges => EFWriteOperationKind.SaveChanges,
                // EF Core 10 reports ExecuteDelete/ExecuteUpdate commands as BulkUpdate,
                // which shares the ExecuteUpdate constant value.
                CommandSource.ExecuteUpdate => EFWriteOperationKind.ExecuteDelete,
                CommandSource.ExecuteDelete => EFWriteOperationKind.ExecuteDelete,
                CommandSource.ExecuteSqlRaw => EFWriteOperationKind.RawSql,
                CommandSource.FromSqlQuery => EFWriteOperationKind.RawSql,
                // Migrations/EnsureCreated DDL is database initialization, not an application
                // write; it passes unbound when no unit of work is active.
                CommandSource.Migrations => EFWriteOperationKind.DatabaseInitialization,
                // Unknown non-query commands are treated conservatively as writes.
                _ => EFWriteOperationKind.UnknownNonQuery
            };
    }
}
