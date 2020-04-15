using Castle.DynamicProxy;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace MiCake.Aop.Castle
{
    internal class CastleMiCakeInterceptorAdaptor : IInterceptor
    {
        private static readonly MethodInfo HandleAsyncWithResultMethodInfo =
            typeof(CastleMiCakeInterceptorAdaptor)
                    .GetMethod(nameof(HandleAsyncWithResult), BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo HandleAsyncMethodInfo =
            typeof(CastleMiCakeInterceptorAdaptor)
                    .GetMethod(nameof(HandleAsync), BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly ConcurrentDictionary<Type, MethodInfo> GenericAsyncHandlers =
            new ConcurrentDictionary<Type, MethodInfo>();

        private IMiCakeInterceptor _miCakeInterceptor;

        public CastleMiCakeInterceptorAdaptor(IMiCakeInterceptor miCakeInterceptor)
        {
            _miCakeInterceptor = miCakeInterceptor;
        }

        public void Intercept(IInvocation invocation)
        {
            MethodType methodType = invocation.Method.GetMethodType();

            var proceedInfo = invocation.CaptureProceedInfo();

            switch (methodType)
            {
                case MethodType.AsyncAction:
                    invocation.ReturnValue = HandleAsyncMethodInfo.Invoke(this, new object[] { invocation, proceedInfo });
                    return;
                case MethodType.AsyncFunction:
                    invocation.ReturnValue = GetHandlerMethod(invocation.Method.ReturnType).Invoke(this, new object[] { invocation, proceedInfo });
                    return;
                default:
                    _miCakeInterceptor.Intercept(new CastleMiCakeInvocationAdaptor(invocation, proceedInfo));
                    return;
            }
        }

        /// <summary>
        /// Gets the <see cref="GenericAsyncHandler"/> for the method invocation <paramref name="returnType"/>.
        /// </summary>
        private MethodInfo GetHandlerMethod(Type returnType)
        {
            MethodInfo handler = GenericAsyncHandlers.GetOrAdd(returnType, CreateHandlerMethod);
            return handler;
        }

        /// <summary>
        /// Creates the generic delegate for the <paramref name="returnType"/> method invocation.
        /// </summary>
        private MethodInfo CreateHandlerMethod(Type returnType)
        {
            Type taskReturnType = returnType.GetGenericArguments()[0];
            return HandleAsyncWithResultMethodInfo.MakeGenericMethod(taskReturnType);
        }

        /// <summary>
        /// This method is created as a delegate and used to make the call to the generic <see cref="IMiCakeInterceptor"/>
        /// </summary>
        /// <paramref name="invocation"/>.</typeparam>
        private async Task HandleAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo)
        {
            await Task.Yield();

            await _miCakeInterceptor.InterceptAsync(
                new CastleMiCakeInvocationAdaptor(invocation, proceedInfo)
            );
        }

        /// <summary>
        /// This method is created as a delegate and used to make the call to the generic  <see cref="IMiCakeInterceptor"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the <see cref="Task{T}"/> <see cref="Task{T}.Result"/> of the method
        /// <paramref name="invocation"/>.</typeparam>
        private async Task<TResult> HandleAsyncWithResult<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo)
        {
            await Task.Yield();

            await _miCakeInterceptor.InterceptAsync(
                new CastleMiCakeInvocationAdaptor(invocation, proceedInfo)
            );

            return await (Task<TResult>)invocation.ReturnValue;
        }
    }
}
