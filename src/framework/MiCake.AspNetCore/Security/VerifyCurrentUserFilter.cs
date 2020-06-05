using MiCake.Core.Util.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Security
{
    internal class VerifyCurrentUserFilter : IAsyncActionFilter
    {
        public VerifyCurrentUserFilter()
        {
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionParams = context.ActionDescriptor.Parameters;

            //Get atturibute parameters.
            var anyCurrentParameter = actionParams.Where(s => s.ParameterType.GetCustomAttribute<CurrentUserAttribute>() != null);
            if (anyCurrentParameter.Count() > 1)
                throw new ArgumentException($"Current action has multiple parameters use {nameof(CurrentUserAttribute)}!");

            if (anyCurrentParameter.Count() == 1)
            {
                var verifyResult = await VerifyCurrentUser(anyCurrentParameter.First(), context.HttpContext, context.ActionArguments);
            }

            await next();
        }


        private async Task<bool> VerifyCurrentUser(
            ParameterDescriptor parameterDescriptor,
            HttpContext httpContext,
            IDictionary<string, object> actionArguments)
        {
            var currentUser = httpContext.User;
            if (currentUser == null)
            {
                await httpContext.ChallengeAsync();
                return false;
            }

            var userIDClaimValue = currentUser.Claims.FirstOrDefault(s => s.Type.ToLower().Equals(VerifyUserClaims.UserID));
            if (userIDClaimValue == null)
            {
                await httpContext.ForbidAsync();
                return false;
            }

            //may be one claim has more value: id:1,2,3
            var candidateValues = userIDClaimValue.Value.Split(',');
            object currentUserTargetValue = null;

            //Get the value of the target parameter
            if (TypeHelper.IsPrimitiveExtended(parameterDescriptor.ParameterType, false, true))
            {
                var typeName = parameterDescriptor.Name;
                actionArguments.TryGetValue(typeName, out currentUserTargetValue);
            }
            else
            {
                var parmeterType = parameterDescriptor.ParameterType;
                var modelEffectivePropertites = parmeterType.GetCustomAttributes<VerifyUserIdAttribute>();

                if (modelEffectivePropertites.Count() > 1)
                    throw new ArgumentException($"Current model: {nameof(parmeterType.Name)} has multiple parameters use {nameof(VerifyUserIdAttribute)}!");

                if (modelEffectivePropertites.Count() > 1)
                    throw new InvalidOperationException($"The current action parameter uses the {nameof(CurrentUserAttribute)}, but {nameof(VerifyUserIdAttribute)} is not used inside the model");

                actionArguments.TryGetValue(parameterDescriptor.Name, out var modelValue);
                if (modelValue != null)
                {
                }
            }

            return true;
        }
    }
}
