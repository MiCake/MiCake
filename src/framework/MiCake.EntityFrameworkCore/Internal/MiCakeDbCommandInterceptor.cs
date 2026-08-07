using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiCake.DDD.Uow;
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
    /// The coordinator is resolved from the provider of the scope that owns the ambient
    /// unit of work; when the write pipeline is unavailable, non-database-initialization
    /// commands are rejected with guidance instead of passing through unmodified.
    /// </summary>
    internal sealed class MiCakeDbCommandInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<MiCakeDbCommandInterceptor> _logger;
        private readonly IUnitOfWorkAmbientAccessor _ambientAccessor;

        public MiCakeDbCommandInterceptor(
            ILogger<MiCakeDbCommandInterceptor> logger,
            IUnitOfWorkAmbientAccessor ambientAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ambientAccessor = ambientAccessor ?? throw new ArgumentNullException(nameof(ambientAccessor));
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            // Permissive: no ambient UoW -> pass through (native EF).
            var coordinator = MiCakeInterceptorPipeline.ResolveCoordinator(_ambientAccessor);
            if (coordinator == null || eventData.Context == null)
            {
                return base.NonQueryExecuting(command, eventData, result);
            }

            var operationKind = Classify(eventData.CommandSource);
            coordinator.BeforeCommand(eventData.Context, command, operationKind);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            // Permissive: no ambient UoW -> pass through (native EF).
            var coordinator = MiCakeInterceptorPipeline.ResolveCoordinator(_ambientAccessor);
            if (coordinator == null || eventData.Context == null)
            {
                return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
            }

            var operationKind = Classify(eventData.CommandSource);
            await coordinator.BeforeCommandAsync(
                    eventData.Context,
                    command,
                    operationKind,
                    cancellationToken)
                .ConfigureAwait(false);
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
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
