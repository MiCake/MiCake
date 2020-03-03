using System;
using System.Threading.Tasks;

namespace MiCake.Aop.Tests.InterfaceProxies
{
    public class TestMiCakeInterceptor : MiCakeInterceptor
    {
        private readonly ListLogger _log;

        public TestMiCakeInterceptor(ListLogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public override void Intercept(IMiCakeInvocation invocation)
        {
            LogInterceptStart(invocation);

            invocation.Proceed();

            LogInterceptEnd(invocation);
        }

        public override async Task InterceptAsync(IMiCakeInvocation invocation)
        {
            LogInterceptStart(invocation);

            await invocation.ProceedAsync();

            LogInterceptEnd(invocation);
        }


        private void LogInterceptStart(IMiCakeInvocation invocation)
        {
            _log.Add($"{invocation.Method.Name}:InterceptStart");
        }

        private void LogInterceptEnd(IMiCakeInvocation invocation)
        {
            _log.Add($"{invocation.Method.Name}:InterceptEnd");
        }
    }
}
