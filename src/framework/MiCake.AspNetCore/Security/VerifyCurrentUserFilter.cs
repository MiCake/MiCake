using MiCake.Core.Util.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Security
{
    internal class VerifyCurrentUserFilter : IAsyncActionFilter
    {
        //The method cache used to get the action parameter as the value of the custom model
        private ConcurrentDictionary<Type, Func<object, object>> _modelActionArgumentGetter = new ConcurrentDictionary<Type, Func<object, object>>();

        public VerifyCurrentUserFilter()
        {
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionParams = context.ActionDescriptor.Parameters;
            bool verifyResult = true;

            //Get atturibute parameters.
            var anyCurrentParameter = actionParams.Where(s => (s as ControllerParameterDescriptor)?.ParameterInfo.GetCustomAttribute<CurrentUserAttribute>() != null);
            if (anyCurrentParameter.Count() > 1)
                throw new ArgumentException($"Current action has multiple parameters use {nameof(CurrentUserAttribute)}!");

            if (anyCurrentParameter.Count() == 1)
            {
                verifyResult = await VerifyCurrentUser(anyCurrentParameter.First(), context.HttpContext, context.ActionArguments);
            }

            if (verifyResult)
            {
                await next();
            }
            else
            {
                //If validation fails,don't execute next filter.
                await context.HttpContext.ForbidAsync();
            }
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

            var userIDClaimValue = currentUser.Claims.FirstOrDefault(s => s.Type.ToLower().Equals(VerifyUserClaims.UserID.ToLower()));
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
                var getValueFunc = _modelActionArgumentGetter.GetOrAdd(parmeterType, factory =>
                 {
                     var modelEffectivePropertites = ReflectionHelper.GetHasAttributeProperties(parmeterType, typeof(VerifyUserIdAttribute));
                     var effectivePropertyCount = modelEffectivePropertites.Count();
                     if (effectivePropertyCount > 1)
                         throw new ArgumentException($"Current model: {nameof(parmeterType.Name)} has multiple parameters use {nameof(VerifyUserIdAttribute)}!");

                     return effectivePropertyCount == 1 ? CreateGetModelValueExpression(parmeterType, modelEffectivePropertites.First()) : null;
                 });

                actionArguments.TryGetValue(parameterDescriptor.Name, out var currentValue);
                currentUserTargetValue = getValueFunc?.Invoke(currentValue);
            }

            return currentUserTargetValue == null ? false : candidateValues.Contains(currentUserTargetValue.ToString());
        }

        private Func<object, object> CreateGetModelValueExpression(Type modelType, PropertyInfo effectiveProperty)
        {
            var funcParam = Expression.Parameter(typeof(object), "model");
            var modelTypeValue = Expression.Convert(funcParam, modelType);
            var readPropertyValue = Expression.Property(modelTypeValue, effectiveProperty);
            var convertResultVaule = Expression.Convert(readPropertyValue, typeof(object));

            return Expression.Lambda<Func<object, object>>(convertResultVaule, funcParam).Compile();
        }
    }
}
