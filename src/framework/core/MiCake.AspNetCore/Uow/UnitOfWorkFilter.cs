using MiCake.AspNetCore.Helper;
using MiCake.Uow;
using MiCake.Uow.Helper;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Uow
{
    public class UnitOfWorkFilter : IAsyncActionFilter
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly MiCakeAspNetUowOption _miCakeAspNetUowOption;

        public UnitOfWorkFilter(
            IUnitOfWorkManager unitOfWorkManager,
            IOptions<MiCakeAspNetOptions> aspnetUowOptions)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _miCakeAspNetUowOption = aspnetUowOptions.Value.UnitOfWork;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!ActionDescriptorHelper.IsControllerActionDescriptor(context.ActionDescriptor))
            {
                await next();
                return;
            }

            var controllerActionDes = ActionDescriptorHelper.AsControllerActionDescriptor(context.ActionDescriptor);
            UnitOfWorkOptions options = _miCakeAspNetUowOption.RootUowOptions;

            if (options == null)
            {
                // create default uow options.
                var currentScope = context.HttpContext.RequestServices as IServiceScope;
                options = new UnitOfWorkOptions()
                {
                    Scope = UnitOfWorkCreateType.Required,
                    ServiceScope = currentScope
                };
            }

            //has disableTransactionAttribute
            var actionMehtod = ActionDescriptorHelper.GetActionMethodInfo(context.ActionDescriptor);
            var hasDisableAttribute = MiCakeUowHelper.IsDisableTransaction(context.Controller.GetType()) ||
                   MiCakeUowHelper.IsDisableTransaction(actionMehtod);

            //has match action key word 
            var hasMatchKeyWord = _miCakeAspNetUowOption.KeyWordToCloseTransaction.Any(keyWord =>
            controllerActionDes.ActionName.ToUpper().StartsWith(keyWord.ToUpper()));

            options.Scope = hasDisableAttribute || hasMatchKeyWord ? UnitOfWorkCreateType.Suppress : options.Scope;

            using var unitOfWork = _unitOfWorkManager.Create(options);
            var result = await next();

            if (Succeed(result))
                await unitOfWork.SaveChangesAsync();
        }

        private static bool Succeed(ActionExecutedContext result)
        {
            return result.Exception == null || result.ExceptionHandled;
        }
    }
}
