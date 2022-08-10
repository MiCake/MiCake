using MiCake.AspNetCore.Helper;
using MiCake.Uow;
using MiCake.Uow.Helper;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.Uow
{
    public class UnitOfWorkAutoSaveFilter : IAsyncActionFilter
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly MiCakeAspNetCoreOptions _options;

        public UnitOfWorkAutoSaveFilter(IUnitOfWorkManager unitOfWorkManager, IOptions<MiCakeAspNetCoreOptions> options)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _options = options.Value;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!ActionDescriptorHelper.IsControllerActionDescriptor(context.ActionDescriptor))
            {
                await next();
                return;
            }

            //has disableTransactionAttribute
            var actionMehtod = ActionDescriptorHelper.GetActionMethodInfo(context.ActionDescriptor);
            var hasDisableAttribute = MiCakeUowHelper.IsDisableAutoUow(context.Controller.GetType()) || MiCakeUowHelper.IsDisableAutoUow(actionMehtod);
            var hasAutoUowAttirbute = MiCakeUowHelper.IsEnableAutoUow(context.Controller.GetType()) || MiCakeUowHelper.IsEnableAutoUow(actionMehtod) || _options.GlobalAutoUowInController;

            var needOpenUow = hasAutoUowAttirbute && !hasDisableAttribute;
            if (needOpenUow)
            {
                using var unitOfWork = _unitOfWorkManager.Create();

                var result = await next();

                if (Succeed(result))
                {
                    await unitOfWork.SaveChangesAsync();
                }
            }
            else
            {
                await next();
            }
        }

        private static bool Succeed(ActionExecutedContext result)
        {
            return result.Exception == null || result.ExceptionHandled;
        }
    }
}
