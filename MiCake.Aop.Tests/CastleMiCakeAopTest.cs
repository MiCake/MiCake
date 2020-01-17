using Castle.DynamicProxy;
using MiCake.Aop.Castle;
using MiCake.Aop.Tests.InterfaceProxies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MiCake.Aop.Tests
{
    public class CastleMiCakeAopTest
    {
        private const string MethodName = nameof(IInterfaceToProxy.SynchronousVoidMethod);
        private readonly ListLogger _log;
        private readonly IInterfaceToProxy _proxy;

        public CastleMiCakeAopTest(ITestOutputHelper output)
        {
            _log = new ListLogger(output);

            _proxy = ProxyGen.CreateProxy(_log, new TestMiCakeInterceptor(_log));
        }
        [Fact]
        public void ShouldLog4Entries()
        {
            // Act
            _proxy.SynchronousVoidMethod();

            // Assert
            Assert.Equal(4, _log.Count);
        }

        [Fact]
        public async Task ShouldAsyncLog4Entries()
        {
            // Act
            await _proxy.AsynchronousVoidMethod();

            // Assert
            Assert.Equal(4, _log.Count);
        }

        [Fact]
        public async Task ShouldAsyncResultLog4Entries()
        {
            // Act
            await _proxy.AsynchronousResultMethod();

            // Assert
            Assert.Equal(4, _log.Count);
        }

        [Fact]
        public async Task ShouldAsyncResultAndVoidLog8Entries()
        {
            // Act
            await _proxy.AsynchronousResultMethod();
            // Act
            await _proxy.AsynchronousVoidMethod();

            // Assert
            Assert.Equal(8, _log.Count);
        }

        [Fact]
        public async Task ShouldDoubleAsyncResultLog8Entries()
        {
            // Act
            await _proxy.AsynchronousResultMethod();
            // Act
            await _proxy.AsynchronousResultMethod();

            // Assert
            Assert.Equal(8, _log.Count);
        }

        [Fact]
        public async Task ShouldThrowException()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(_proxy.AsynchronousVoidExceptionMethod)
                .ConfigureAwait(false);
        }
    }



    public static class ProxyGen
    {
        public static readonly IProxyGenerator Generator = new ProxyGenerator();

        public static IInterfaceToProxy CreateProxy(ListLogger log, IMiCakeInterceptor miCakeInterceptor)
        {
            return CreateProxy(log, miCakeInterceptor, out _);
        }

        public static IInterfaceToProxy CreateProxy(
            ListLogger log,
            IMiCakeInterceptor miCakeInterceptor,
            out ClassWithInterfaceToProxy target)
        {
            var localTarget = new ClassWithInterfaceToProxy(log);
            target = localTarget;
            return CreateProxy(() => localTarget, miCakeInterceptor);
        }

        public static IInterfaceToProxy CreateProxy(Func<IInterfaceToProxy> factory, IMiCakeInterceptor miCakeInterceptor)
        {
            IInterfaceToProxy implementation = factory();
            IMiCakeProxy castleMiCakeProxy = new CastleMiCakeProxyProvider().GetMiCakeProxy();
            IInterfaceToProxy proxy = castleMiCakeProxy.CreateInterfaceProxyWithTarget<IInterfaceToProxy>(implementation, miCakeInterceptor);
            return proxy;
        }
    }
}
