using System.Threading.Tasks;

namespace MiCake.Aop
{
    public abstract class MiCakeInterceptor : IMiCakeInterceptor
    {
        /// <summary>
        /// If the intercepted method is synchronous, the method will be executed
        /// </summary>
        /// <param name="invocation"><see cref=" IMiCakeInvocation"/></param>
        public abstract void Intercept(IMiCakeInvocation invocation);

        /// <summary>
        /// If the intercepted method is asynchronous, the method is executed.
        /// asynchronous method is main: return type is <see cref="Task"/> or <see cref="Task{TResult}"/>
        /// </summary>
        /// <param name="invocation"><see cref=" IMiCakeInvocation"/></param>
        public abstract Task InterceptAsync(IMiCakeInvocation invocation);
    }
}
