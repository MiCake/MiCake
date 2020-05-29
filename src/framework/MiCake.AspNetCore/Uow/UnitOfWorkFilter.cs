using MiCake.AspNetCore.Helper;
using MiCake.Uow;
using MiCake.Uow.Helper;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Uow
{
    public class UnitOfWorkFilter : IAsyncActionFilter
    {
        private IUnitOfWorkManager _unitOfWorkManager;
        private MiCakeAspNetUowOption _miCakeAspNetUowOption;

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
            UnitOfWorkOptions options = _miCakeAspNetUowOption.RootUowOptions ?? new UnitOfWorkOptions();

            if (options.Scope != UnitOfWorkScope.Suppress)
            {
                //has disableTransactionAttribute
                var actionMehtod = ActionDescriptorHelper.GetActionMethodInfo(context.ActionDescriptor);
                var hasDisableAttribute = MiCakeUowHelper.IsDisableTransaction(context.Controller.GetType()) ||
                       MiCakeUowHelper.IsDisableTransaction(actionMehtod);

                //has match action key word 
                var hasMatchKeyWord = _miCakeAspNetUowOption.KeyWordToCloseTransaction.Any(keyWord =>
                controllerActionDes.ActionName.ToUpper().StartsWith(keyWord.ToUpper()));

                bool needCloseTransaction = hasDisableAttribute || hasMatchKeyWord;
                options.Scope = needCloseTransaction ? UnitOfWorkScope.Suppress : UnitOfWorkScope.Required;
            }

            using (var unitOfWork = _unitOfWorkManager.Create(options))
            {
                var result = await next();

                if (Succeed(result))
                    await unitOfWork.SaveChangesAsync();
            }
        }

        private static bool Succeed(ActionExecutedContext result)
        {
            return result.Exception == null || result.ExceptionHandled;
        }
    }
}
