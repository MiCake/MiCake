using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Aop
{
    /// <summary>
    /// Provides the main DynamicProxy extension point that allows member interception.
    /// </summary>
    public interface IMiCakeInterceptor
    {
        void Intercept(IMiCakeInvocation invocation);

        Task InterceptAsync(IMiCakeInvocation invocation);
    }
}
