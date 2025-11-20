using MiCake.AspNetCore.Helper;
using MiCake.DDD.Uow;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Uow
{
    /// <summary>
    /// Action filter that automatically manages Unit of Work for controller actions.
    /// Creates, commits or rolls back UoW based on action execution result and configured options.
    /// Supports declarative configuration via [UnitOfWork] attribute.
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

            var controllerActionDes = ActionDescriptorHelper.AsControllerActionDescriptor(context.ActionDescriptor);

            // Check for UnitOfWork attribute on action, controller, or endpoint metadata
            UnitOfWorkAttribute? uowAttribute;
            try
            {
                uowAttribute = GetUnitOfWorkAttribute(controllerActionDes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UnitOfWork attribute");
                throw;
            }

            // Determine if UoW should be enabled
            // If attribute is present, it determines enablement (including DisableUnitOfWorkAttribute)
            // Otherwise, use global configuration
            bool isUowEnabled = uowAttribute?.IsUowEnabled ?? _uowOptions.IsAutoUowEnabled;

            if (!isUowEnabled)
            {
                _logger.LogDebug("Unit of Work disabled for action {ActionName}", controllerActionDes.ActionName);
                await next().ConfigureAwait(false);
                return;
            }

            // Determine if this is a read-only action based on naming convention
            bool isReadOnly = DetermineIfReadOnly(controllerActionDes.ActionName);

            // Create UoW options
            UnitOfWorkOptions options;
            if (uowAttribute != null)
            {
                // Use attribute settings
                options = uowAttribute.CreateOptions();
                if (isReadOnly)
                {
                    options.IsReadOnly = true;
                }
            }
            else
            {
                // Use default settings
                options = isReadOnly ? UnitOfWorkOptions.ReadOnly : UnitOfWorkOptions.Default;
            }

            IUnitOfWork? unitOfWork = null;
            try
            {
                unitOfWork = await _unitOfWorkManager.BeginAsync(options, requiresNew: false, context.HttpContext.RequestAborted).ConfigureAwait(false);

                _logger.LogDebug(
                    "Started Unit of Work {UowId} for action {ActionName}. IsReadOnly: {IsReadOnly}, InitMode: {InitMode}",
                    unitOfWork.Id,
                    controllerActionDes.ActionName,
                    isReadOnly,
                    options.InitializationMode);

                // Execute the action
                var result = await next().ConfigureAwait(false);

                // Handle UoW based on action execution result
                if (ActionSucceeded(result))
                {
                    // Action succeeded - commit unless read-only
                    if (!isReadOnly)
                    {
                        await unitOfWork.CommitAsync().ConfigureAwait(false);

                        _logger.LogDebug(
                            "Committed Unit of Work {UowId} for action {ActionName}",
                            unitOfWork.Id,
                            controllerActionDes.ActionName);
                    }
                    else
                    {
                        // Mark as completed for read-only (no actual commit needed)
                        await unitOfWork.MarkAsCompletedAsync().ConfigureAwait(false);

                        _logger.LogDebug(
                            "Marked read-only Unit of Work {UowId} as completed for action {ActionName}",
                            unitOfWork.Id,
                            controllerActionDes.ActionName);
                    }
                }
                else if (result.Exception != null)
                {
                    // Action failed with exception - rollback
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
                // Exception during UoW management - attempt rollback
                if (unitOfWork != null && !unitOfWork.IsCompleted)
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
