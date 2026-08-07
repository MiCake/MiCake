using Microsoft.Extensions.Logging;
using MiCake.DDD.Uow.Exceptions;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Shared execution core for isolated unit-of-work boundaries (requiresNew and standalone).
    /// Runs the operation against the isolated provider, commits on success, rolls back on
    /// failure, and always disposes asynchronously. Failures are composed into a single
    /// boundary exception without losing the primary exception.
    /// </summary>
    internal static class IsolatedUowExecution
    {
        public static async Task<TResult> ExecuteAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            IServiceProvider provider,
            IUnitOfWork unitOfWork,
            ILogger logger,
            string operationName,
            CancellationToken cancellationToken)
        {
            TResult result = default!;
            ExceptionDispatchInfo? primaryEDI = null;
            var rollbackFailures = new List<Exception>();
            Exception? cleanupFailure = null;

            try
            {
                result = await operation(provider, cancellationToken).ConfigureAwait(false);
                await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (PartialUnitOfWorkCommitException ex)
            {
                // Partial commit is a terminal state: no overall rollback is attempted and no
                // rolled-back event may be raised; the original exception propagates as-is.
                primaryEDI = ExceptionDispatchInfo.Capture(ex);
            }
            catch (Exception ex)
            {
                primaryEDI = ExceptionDispatchInfo.Capture(ex);
                if (!unitOfWork.IsCompleted)
                {
                    try
                    {
                        // Cleanup rollback must not be cancelled by the operation's token.
                        await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (UnitOfWorkBoundaryException rollbackEx)
                    {
                        rollbackFailures.AddRange(rollbackEx.RollbackExceptions);
                    }
                    catch (Exception rollbackEx)
                    {
                        rollbackFailures.Add(rollbackEx);
                    }
                }
            }
            finally
            {
                try
                {
                    await unitOfWork.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure = ex;
                    logger.LogError(ex, "Failed to dispose isolated UnitOfWork {UnitOfWorkId}", unitOfWork.Id);
                }
            }

            if (cleanupFailure != null)
            {
                throw new UnitOfWorkBoundaryException(
                    $"The {operationName} operation completed but cleanup of the unit of work failed.",
                    primaryEDI?.SourceException,
                    rollbackFailures.Count > 0 ? rollbackFailures : null,
                    [cleanupFailure]);
            }

            if (rollbackFailures.Count > 0)
            {
                throw new UnitOfWorkBoundaryException(
                    $"The {operationName} operation failed and rollback of eligible resources also failed.",
                    primaryEDI?.SourceException,
                    rollbackFailures);
            }

            primaryEDI?.Throw();
            return result;
        }
    }
}
