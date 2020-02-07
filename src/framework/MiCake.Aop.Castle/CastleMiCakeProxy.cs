using System;
using Castle.DynamicProxy;
using System.Linq;

namespace MiCake.Aop.Castle
{
    public class CastleMiCakeProxy : IMiCakeProxy
    {
        private ProxyGenerator _proxyGenerator = new ProxyGenerator();

        public CastleMiCakeProxy()
        {
        }

        public object CreateClassProxy(Type classToProxy, params IMiCakeInterceptor[] interceptors)
            => _proxyGenerator.CreateClassProxy(classToProxy, AdaptorMiCakeInterceptorsToCastle(interceptors));

        public TClass CreateClassProxy<TClass>(params IMiCakeInterceptor[] interceptors) where TClass : class
            => _proxyGenerator.CreateClassProxy<TClass>(AdaptorMiCakeInterceptorsToCastle(interceptors));

        public object CreateClassProxyWithTarget(Type classToProxy, object target, params IMiCakeInterceptor[] interceptors)
        {
            if (interceptors.Length == 0)
                return target;

            return _proxyGenerator.CreateClassProxyWithTarget(classToProxy, target, AdaptorMiCakeInterceptorsToCastle(interceptors));
        }

        public TClass CreateClassProxyWithTarget<TClass>(object target, params IMiCakeInterceptor[] interceptors) where TClass : class
        {
            if (interceptors.Length == 0)
                return (TClass)target;

            return _proxyGenerator.CreateClassProxyWithTarget<TClass>((TClass)target, AdaptorMiCakeInterceptorsToCastle(interceptors));
        }

        public object CreateInterfaceProxy(Type interfaceToProxy, params IMiCakeInterceptor[] interceptors)
            => _proxyGenerator.CreateInterfaceProxyWithoutTarget(interfaceToProxy, AdaptorMiCakeInterceptorsToCastle(interceptors));

        public TInterface CreateInterfaceProxy<TInterface>(params IMiCakeInterceptor[] interceptors) where TInterface : class
            => _proxyGenerator.CreateInterfaceProxyWithoutTarget<TInterface>(AdaptorMiCakeInterceptorsToCastle(interceptors));

        public object CreateInterfaceProxyWithTarget(Type interfaceToProxy, object target, params IMiCakeInterceptor[] interceptors)
        {
            if (interceptors.Length == 0)
                return target;

            return _proxyGenerator.CreateInterfaceProxyWithTarget(interfaceToProxy, target, AdaptorMiCakeInterceptorsToCastle(interceptors));
        }

        public TInterface CreateInterfaceProxyWithTarget<TInterface>(object target, params IMiCakeInterceptor[] interceptors) where TInterface:class
        {
            if (interceptors.Length == 0)
                return (TInterface)target;

            return _proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>((TInterface)target, AdaptorMiCakeInterceptorsToCastle(interceptors));
        }

        protected virtual IInterceptor[] AdaptorMiCakeInterceptorsToCastle(IMiCakeInterceptor[] miCakeInterceptors)
        {
            if (miCakeInterceptors.Length == 0)
                return new IInterceptor[0];

            return miCakeInterceptors.AsEnumerable()
                                    .Select(micakeInterceptor => new CastleMiCakeInterceptorAdaptor(micakeInterceptor))
                                    .ToArray();
        }
    }
}
