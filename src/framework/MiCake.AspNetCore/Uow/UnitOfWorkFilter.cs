using MiCake.AspNetCore.Helper;
using MiCake.DDD.Uow;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Uow
{
    /// <summary>
    /// Action filter that automatically manages Unit of Work for controller actions.
    /// Creates, commits or rolls back UoW based on action execution result and configured options.
    /// Supports declarative configuration via [UnitOfWork] attribute.
    /// </summary>
    public class UnitOfWorkFilter : IAsyncActionFilter, IOrderedFilter
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly MiCakeAspNetUowOptions _uowOptions;
        private readonly ILogger<UnitOfWorkFilter> _logger;

        public int Order => int.MaxValue; // Run last

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

            var controllerActionDes = ActionDescriptorHelper.AsControllerActionDescriptor(context.ActionDescriptor);

            // An explicit disable attribute always wins over global configuration
            if (HasDisableUnitOfWorkAttribute(controllerActionDes))
            {
                _logger.LogDebug("Unit of Work disabled for action {ActionName}", controllerActionDes.ActionName);
                await next().ConfigureAwait(false);
                return;
            }

            // Determine if UoW should be enabled
            // If attribute is present, it enables UoW; otherwise use global configuration
            var uowAttribute = GetUnitOfWorkAttribute(controllerActionDes);
            bool isUowEnabled = uowAttribute != null || _uowOptions.EnableAutoUnitOfWork;

            if (!isUowEnabled)
            {
                _logger.LogDebug("Unit of Work disabled for action {ActionName}", controllerActionDes.ActionName);
                await next().ConfigureAwait(false);
                return;
            }

            // Determine if this is a read-only operation.
            // Explicit action/controller metadata wins; action-name inference only applies when opted in.
            bool isReadOnly;
            if (uowAttribute != null)
            {
                isReadOnly = uowAttribute.IsReadOnly;
            }
            else
            {
                isReadOnly = _uowOptions.EnableReadOnlyActionNameInference &&
                             DetermineIfReadOnly(controllerActionDes.ActionName);
            }

            // Create UoW options
            var options = CreateOptions(uowAttribute, isReadOnly);

            // Execute the entire unit of work flow in a helper to reduce cognitive complexity
            await ExecuteWithinUnitOfWorkAsync(controllerActionDes, options, isReadOnly, context.HttpContext.RequestAborted, next).ConfigureAwait(false);
        }

        private static UnitOfWorkOptions CreateOptions(UnitOfWorkAttribute? uowAttribute, bool isReadOnly)
        {
            if (uowAttribute != null)
            {
                var options = new UnitOfWorkOptions
                {
                    IsReadOnly = uowAttribute.IsReadOnly
                };

                if (uowAttribute.IsolationLevel.HasValue)
                {
                    options.IsolationLevel = uowAttribute.IsolationLevel;
                }

                return options;
            }

            return isReadOnly ? UnitOfWorkOptions.ReadOnly : UnitOfWorkOptions.Default;
        }

        private async Task ExecuteWithinUnitOfWorkAsync(
            ControllerActionDescriptor controllerActionDes,
            UnitOfWorkOptions options,
            bool isReadOnly,
            CancellationToken cancellationToken,
            ActionExecutionDelegate next)
        {
            IUnitOfWork? unitOfWork = null;
            Exception? exceptionDuringBody = null;
            ExceptionDispatchInfo? disposeFailureEDI = null;
            try
            {
                unitOfWork = await _unitOfWorkManager.BeginAsync(options, cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(
                    "Started Unit of Work {UowId} for action {ActionName}. IsReadOnly: {IsReadOnly}, InitMode: {InitMode}",
                    unitOfWork.Id,
                    controllerActionDes.ActionName,
                    isReadOnly,
                    options.InitializationMode);

                // Execute the action
                var result = await next().ConfigureAwait(false);

                // Handle UoW based on action execution result.
                // Commit/rollback intentionally do not take the request cancellation token:
                // request cancellation is already detected via ActionExecutedContext.Canceled above,
                // and interrupting an in-flight commit could leave resources in a partial-commit state.
                if (ActionSucceeded(result))
                {
                    await HandleSuccessAsync(unitOfWork, controllerActionDes, isReadOnly).ConfigureAwait(false);
                }
                else
                {
                    await HandleFailureAsync(unitOfWork, result, controllerActionDes).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                exceptionDuringBody = ex;

                // Best-effort rollback; rethrow to preserve the original stack.
                await HandleBodyExceptionAsync(unitOfWork, ex).ConfigureAwait(false);
                throw;
            }
            finally
            {
                // Dispose without throwing from finally; failures are captured for rethrow.
                disposeFailureEDI = await DisposeUnitOfWorkAsync(unitOfWork, exceptionDuringBody).ConfigureAwait(false);
            }

            // Surface a dispose failure captured after a successful body (never from finally).
            disposeFailureEDI?.Throw();
        }

        /// <summary>
        /// Best-effort rollback after a UoW management exception. A rollback failure is
        /// composed with the original exception into an <see cref="AggregateException"/>.
        /// </summary>
        private async Task HandleBodyExceptionAsync(IUnitOfWork? unitOfWork, Exception ex)
        {
            if (unitOfWork == null || unitOfWork.IsCompleted)
            {
                return;
            }

            try
            {
                // Explicit opt-out: compensation rollback must complete even when
                // the request is cancelled, so no transaction is left dangling.
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
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

                throw new AggregateException(
                    "Unit of Work operation failed and rollback also failed",
                    ex, rollbackEx);
            }
        }

        /// <summary>
        /// Disposes the unit of work asynchronously. After a body failure a dispose failure is
        /// only logged; otherwise it is returned as an <see cref="ExceptionDispatchInfo"/> for
        /// the caller to rethrow after the finally block.
        /// </summary>
        private async Task<ExceptionDispatchInfo?> DisposeUnitOfWorkAsync(IUnitOfWork? unitOfWork, Exception? exceptionDuringBody)
        {
            if (unitOfWork == null)
            {
                return null;
            }

            try
            {
                await unitOfWork.DisposeAsync().ConfigureAwait(false);
                return null;
            }
            catch (Exception disposeEx)
            {
                if (exceptionDuringBody != null)
                {
                    _logger.LogError(
                        disposeEx,
                        "Unit of Work {UowId} dispose failed after an earlier failure; preserving the original exception",
                        unitOfWork.Id);
                    return null;
                }

                _logger.LogError(
                    disposeEx,
                    "Unit of Work {UowId} dispose failed after a successful operation",
                    unitOfWork.Id);
                return ExceptionDispatchInfo.Capture(disposeEx);
            }
        }

        /// <summary>
        /// Completes a successful action: commits a writable unit of work, or marks a
        /// read-only unit of work as completed without a physical commit.
        /// </summary>
        private async Task HandleSuccessAsync(
            IUnitOfWork unitOfWork,
            ControllerActionDescriptor controllerActionDes,
            bool isReadOnly)
        {
            if (!isReadOnly)
            {
                // Explicit opt-out: commit must complete even when the request is cancelled,
                // otherwise resources could be left in a partial-commit state.
                await unitOfWork.CommitAsync(CancellationToken.None).ConfigureAwait(false);

                _logger.LogDebug(
                    "Committed Unit of Work {UowId} for action {ActionName}",
                    unitOfWork.Id,
                    controllerActionDes.ActionName);
            }
            else
            {
                // Mark as completed for read-only (no actual commit needed). Explicit
                // opt-out: completion must not be interrupted by request cancellation.
                await unitOfWork.MarkAsCompletedAsync(CancellationToken.None).ConfigureAwait(false);

                _logger.LogDebug(
                    "Marked read-only Unit of Work {UowId} as completed for action {ActionName}",
                    unitOfWork.Id,
                    controllerActionDes.ActionName);
            }
        }

        /// <summary>
        /// Rolls back the unit of work after a failed or canceled action and logs the reason.
        /// </summary>
        private async Task HandleFailureAsync(
            IUnitOfWork unitOfWork,
            ActionExecutedContext result,
            ControllerActionDescriptor controllerActionDes)
        {
            // Explicit opt-out: rollback must complete even when the request is cancelled,
            // otherwise resources could be left in a partial-commit state.
            await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);

            if (result.Exception != null && !result.ExceptionHandled)
            {
                _logger.LogWarning(
                    result.Exception,
                    "Rolled back Unit of Work {UowId} for action {ActionName} due to exception",
                    unitOfWork.Id,
                    controllerActionDes.ActionName);
            }
            else
            {
                _logger.LogDebug(
                    "Rolled back Unit of Work {UowId} for canceled action {ActionName}",
                    unitOfWork.Id,
                    controllerActionDes.ActionName);
            }
        }

        /// <summary>
        /// Determines if the action execution succeeded.
        /// </summary>
        /// <param name="result">The action execution result</param>
        /// <returns>True if action succeeded, false otherwise</returns>
        private static bool ActionSucceeded(ActionExecutedContext result)
        {
            return (result.Exception == null || result.ExceptionHandled) && !result.Canceled;
        }

        /// <summary>
        /// Gets the UnitOfWork attribute from the action method, controller type, or endpoint metadata.
        /// Action-level attribute takes precedence over controller-level, then endpoint metadata.
        /// </summary>
        private static UnitOfWorkAttribute? GetUnitOfWorkAttribute(ControllerActionDescriptor controllerActionDes)
        {
            // Check action method first
            if (controllerActionDes.MethodInfo != null)
            {
                var actionAttribute = controllerActionDes.MethodInfo.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true);
                if (actionAttribute != null)
                {
                    return actionAttribute;
                }
            }

            // Check controller type
            if (controllerActionDes.ControllerTypeInfo != null)
            {
                var controllerAttribute = controllerActionDes.ControllerTypeInfo.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true);
                if (controllerAttribute != null)
                {
                    return controllerAttribute;
                }
            }

            // Check endpoint metadata
            if (controllerActionDes.EndpointMetadata != null)
            {
                foreach (var metadata in controllerActionDes.EndpointMetadata)
                {
                    if (metadata is UnitOfWorkAttribute uowAttribute)
                    {
                        return uowAttribute;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the action, controller, or endpoint metadata carries a
        /// <see cref="DisableUnitOfWorkAttribute"/>, which explicitly opts out of UoW management.
        /// </summary>
        private static bool HasDisableUnitOfWorkAttribute(ControllerActionDescriptor controllerActionDes)
        {
            if (controllerActionDes.MethodInfo?.GetCustomAttribute<DisableUnitOfWorkAttribute>(inherit: true) != null)
            {
                return true;
            }

            if (controllerActionDes.ControllerTypeInfo?.GetCustomAttribute<DisableUnitOfWorkAttribute>(inherit: true) != null)
            {
                return true;
            }

            return controllerActionDes.EndpointMetadata?.OfType<DisableUnitOfWorkAttribute>().Any() ?? false;
        }

        /// <summary>
        /// Determines if the action should be treated as read-only based on naming keywords.
        /// </summary>
        private bool DetermineIfReadOnly(string actionName)
        {
            // Check if action name starts with read-only keywords
            return _uowOptions.ReadOnlyActionKeywords.Any(keyword =>
                actionName.StartsWith(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}
