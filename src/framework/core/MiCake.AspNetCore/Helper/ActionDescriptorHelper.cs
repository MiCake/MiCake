using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace MiCake.AspNetCore.Helper
{
    internal static class ActionDescriptorHelper
    {
        public static bool IsControllerActionDescriptor(ActionDescriptor actionDecriptor)
        {
            return actionDecriptor is ControllerActionDescriptor;
        }

        public static ControllerActionDescriptor AsControllerActionDescriptor(ActionDescriptor actionDescriptor)
        {
            if (actionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
                throw new InvalidCastException($"{nameof(ActionDescriptor)} cannot convert to {nameof(ControllerActionDescriptor)}," +
                    $"Because of {nameof(ActionDescriptor)} type is : {actionDescriptor.GetType().Name}");

            return controllerActionDescriptor;
        }

        public static MethodInfo GetActionMethodInfo(ActionDescriptor actionDescriptor)
        {
            return AsControllerActionDescriptor(actionDescriptor).MethodInfo;
        }
    }
}
