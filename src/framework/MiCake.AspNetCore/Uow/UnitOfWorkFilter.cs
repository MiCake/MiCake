using MiCake.AspNetCore.Helper;
using MiCake.DDD.Uow;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Uow
{
    /// <summary>
    /// Action filter that automatically manages Unit of Work for controller actions.
    /// Creates, commits or rolls back UoW based on action execution result and configured options.
    /// </summary>
    public class UnitOfWorkFilter : IAsyncActionFilter
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly MiCakeAspNetUowOption _uowOptions;
        private readonly ILogger<UnitOfWorkFilter> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkFilter"/> class.
        /// </summary>
        /// <param name="unitOfWorkManager">Unit of work manager</param>
        /// <param name="aspnetUowOptions">ASP.NET Core UoW options</param>
        /// <param name="logger">Logger instance</param>
        public UnitOfWorkFilter(
            IUnitOfWorkManager unitOfWorkManager,
            IOptions<MiCakeAspNetOptions> aspnetUowOptions,
            ILogger<UnitOfWorkFilter> logger)
        {
            _unitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
            _uowOptions = aspnetUowOptions?.Value?.UnitOfWork ?? throw new ArgumentNullException(nameof(aspnetUowOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the action filter asynchronously.
        /// </summary>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            // Only process controller actions
            if (!ActionDescriptorHelper.IsControllerActionDescriptor(context.ActionDescriptor))
            {
                await next().ConfigureAwait(false);
                return;
            }

            // If auto-begin is disabled, skip UoW management
            if (!_uowOptions.IsAutoBeginEnabled)
            {
                await next().ConfigureAwait(false);
                return;
            }

            var controllerActionDes = ActionDescriptorHelper.AsControllerActionDescriptor(context.ActionDescriptor);
            
            // Check if this is a read-only action based on configured keywords
            var isReadOnlyAction = _uowOptions.KeyWordForCloseAutoCommit.Any(keyWord =>
                controllerActionDes.ActionName.StartsWith(keyWord, StringComparison.OrdinalIgnoreCase));

            IUnitOfWork? unitOfWork = null;
            try
            {
                // Begin a new Unit of Work (transaction starts immediately in the new design)
                unitOfWork = _unitOfWorkManager.Begin();

                _logger.LogDebug(
                    "Started Unit of Work {UowId} for action {ActionName}. IsReadOnly: {IsReadOnly}",
                    unitOfWork.Id,
                    controllerActionDes.ActionName,
                    isReadOnlyAction);

                // If this is a read-only action, mark UoW as completed to skip commit
                // This optimizes performance by avoiding unnecessary SaveChanges
                if (isReadOnlyAction)
                {
                    await unitOfWork.MarkAsCompletedAsync().ConfigureAwait(false);
                }

                // Execute the action
                var result = await next().ConfigureAwait(false);

                // Handle UoW based on action execution result
                if (ActionSucceeded(result))
                {
                    // Action succeeded - commit if auto-commit is enabled and not read-only
                    if (_uowOptions.IsAutoCommitEnabled && !isReadOnlyAction)
                    {
                        await unitOfWork.CommitAsync().ConfigureAwait(false);
                        
                        _logger.LogDebug(
                            "Committed Unit of Work {UowId} for action {ActionName}",
                            unitOfWork.Id,
                            controllerActionDes.ActionName);
                    }
                }
                else if (_uowOptions.IsAutoRollbackEnabled && result.Exception != null)
                {
                    // Action failed with exception - rollback if auto-rollback is enabled
                    await unitOfWork.RollbackAsync().ConfigureAwait(false);
                    
                    _logger.LogWarning(
                        result.Exception,
                        "Rolled back Unit of Work {UowId} for action {ActionName} due to exception",
                        unitOfWork.Id,
                        controllerActionDes.ActionName);
                }
            }
            catch (Exception ex)
            {
                // Exception during UoW management
                if (unitOfWork != null && _uowOptions.IsAutoRollbackEnabled)
                {
                    try
                    {
                        await unitOfWork.RollbackAsync().ConfigureAwait(false);
                        _logger.LogWarning(
                            ex,
                            "Rolled back Unit of Work {UowId} due to exception during filter execution",
                            unitOfWork.Id);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(
                            rollbackEx,
                            "Failed to rollback Unit of Work {UowId}",
                            unitOfWork.Id);
                    }
                }
                
                throw;
            }
            finally
            {
                // Dispose the Unit of Work
                unitOfWork?.Dispose();
            }
        }

        /// <summary>
        /// Determines if the action execution succeeded.
        /// </summary>
        /// <param name="result">The action execution result</param>
        /// <returns>True if action succeeded, false otherwise</returns>
        private static bool ActionSucceeded(ActionExecutedContext result)
        {
            return result.Exception == null || result.ExceptionHandled;
        }
    }
}
