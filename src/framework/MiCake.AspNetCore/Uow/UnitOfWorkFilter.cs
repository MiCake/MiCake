using MiCake.AspNetCore.Helper;
using MiCake.DDD.Uow;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Uow
{
    public class UnitOfWorkFilter(
        IUnitOfWorkManager unitOfWorkManager,
        IOptions<MiCakeAspNetOptions> aspnetUowOptions) : IAsyncActionFilter
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager = unitOfWorkManager;
        private readonly MiCakeAspNetUowOption _miCakeAspNetUowOption = aspnetUowOptions.Value.UnitOfWork;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!ActionDescriptorHelper.IsControllerActionDescriptor(context.ActionDescriptor))
            {
                await next();
                return;
            }

            var controllerActionDes = ActionDescriptorHelper.AsControllerActionDescriptor(context.ActionDescriptor);

            //has match action key word 
            var noNeedCommit = _miCakeAspNetUowOption.KeyWordForCloseAutoCommit.Any(keyWord =>
                controllerActionDes.ActionName.StartsWith(keyWord, System.StringComparison.CurrentCultureIgnoreCase));

            using var unitOfWork = _unitOfWorkManager.Begin();
            if (noNeedCommit)
            {
                await unitOfWork.MarkAsCompletedAsync();
            }

            var result = await next();

            if (Succeed(result) && !noNeedCommit)
                await unitOfWork.CommitAsync();
        }

        private static bool Succeed(ActionExecutedContext result)
        {
            return result.Exception == null || result.ExceptionHandled;
        }
    }
}
