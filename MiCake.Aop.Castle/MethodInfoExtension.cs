using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiCake.Aop.Castle
{
    internal static class MethodInfoExtension
    {
        /// <summary>
        /// Gets the <see cref="MethodType"/> based upon the <paramref name="returnType"/> of the method invocation.
        /// </summary>
        internal static MethodType GetMethodType(this MethodInfo methodInfo)
        {
            Type returnType = methodInfo.ReturnType;

            // If there's no return type, or it's not a task, then assume it's a synchronous method.
            if (returnType == typeof(void) || !typeof(Task).IsAssignableFrom(returnType))
                return MethodType.Synchronous;

            // The return type is a task of some sort, so assume it's asynchronous
            return returnType.GetTypeInfo().IsGenericType ? MethodType.AsyncFunction : MethodType.AsyncAction;
        }
    }
}
