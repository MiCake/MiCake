using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Aop
{
    /// <summary>
    /// Provides proxy objects for classes and interfaces.
    /// </summary>
    public interface IMiCakeProxy
    {
        /// <summary>
        ///  Creates proxy object intercepting calls to virtual members of type classToProxy
        ///  on newly created instance of that type with given interceptors.
        /// </summary>
        /// <param name="classToProxy">Type of class which will be proxied.</param>
        /// <param name="interceptors">The interceptors called during the invocation of proxied methods.</param>
        /// <returns>New object of type classToProxy proxying calls to virtual members of classToProxy type.</returns>
        object CreateClassProxy(Type classToProxy, params IMiCakeInterceptor[] interceptors);

        /// <summary>
        ///  Creates proxy object intercepting calls to virtual members of type classToProxy
        ///  on newly created instance of that type with given interceptors.
        /// </summary>
        /// <typeparam name="TClass">Type of class which will be proxied.</typeparam>
        /// <param name="interceptors">The interceptors called during the invocation of proxied methods.</param>
        /// <returns>New object of type classToProxy proxying calls to virtual members of classToProxy type.</returns>
        TClass CreateClassProxy<TClass>(params IMiCakeInterceptor[] interceptors) where TClass : class;

        /// <summary>
        /// Creates proxy object intercepting calls to virtual members of type classToProxy
        /// on newly created instance of that type with given interceptors.
        /// </summary>
        /// <param name="classToProxy">Type of class which will be proxied</param>
        /// <param name="target">The target object, calls to which will be intercepted.</param>
        /// <param name="interceptors">The interceptors called during the invocation of proxied methods.</param>
        /// <returns>New object of type classToProxy proxying calls to virtual members of classToProxy type.</returns>
        object CreateClassProxyWithTarget(Type classToProxy, object target, params IMiCakeInterceptor[] interceptors);

        /// <summary>
        ///  Creates proxy object intercepting calls to virtual members of type TClass on
        ///  newly created instance of that type with given interceptors.
        /// </summary>
        /// <typeparam name="TClass">Type of class which will be proxied.</typeparam>
        /// <param name="target">The target object, calls to which will be intercepted.</param>
        /// <param name="interceptors">The interceptors called during the invocation of proxied methods.</param>
        /// <returns>New object of type TClass proxying calls to virtual members of TClass type.</returns>
        TClass CreateClassProxyWithTarget<TClass>(object target, params IMiCakeInterceptor[] interceptors) where TClass : class;

        /// <summary>
        ///  Creates proxy object intercepting calls to members of interface TInterface on
        ///  target object generated at runtime with given interceptor.
        /// </summary>
        /// <param name="interfaceToProxy">Type of the interface which will be proxied.</param>
        /// <param name="interceptors">Type of the interface which will be proxied.</param>
        /// <returns>Object proxying calls to members of TInterface types on generated target object.</returns>
        object CreateInterfaceProxy(Type interfaceToProxy, params IMiCakeInterceptor[] interceptors);

        /// <summary>
        ///  Creates proxy object intercepting calls to members of interface TInterface on
        ///  target object generated at runtime with given interceptor.
        /// </summary>
        /// <typeparam name="TInterface">Type of the interface which will be proxied.</typeparam>
        /// <param name="interceptors">The interceptors called during the invocation of proxied methods.</param>
        /// <returns>Object proxying calls to members of TInterface types on generated target object.</returns>
        TInterface CreateInterfaceProxy<TInterface>(params IMiCakeInterceptor[] interceptors) where TInterface : class;

        /// <summary>
        /// Creates proxy object intercepting calls to members of interface TInterface on
        /// target object generated at runtime with given interceptor.
        /// </summary>
        /// <param name="interfaceToProxy">Type of the interface which will be proxied.</param>
        /// <param name="target">The target object, calls to which will be intercepted.</param>
        /// <param name="interceptors">The interceptors called during the invocation of proxied methods.</param>
        /// <returns>Object proxying calls to members of interfaceToProxy types on generated target object.</returns>
        object CreateInterfaceProxyWithTarget(Type interfaceToProxy, object target, params IMiCakeInterceptor[] interceptors);

        /// <summary>
        /// Creates proxy object intercepting calls to members of interface TInterface on
        /// target object generated at runtime with given interceptor.
        /// </summary>
        /// <typeparam name="TInterface">Type of the interface which will be proxied.</typeparam>
        /// <param name="target">The target object, calls to which will be intercepted.</param>
        /// <param name="interceptors">The interceptors called during the invocation of proxied methods.</param>
        /// <returns>Object proxying calls to members of interfaceToProxy types on generated target object.</returns>
        TInterface CreateInterfaceProxyWithTarget<TInterface>(object target, params IMiCakeInterceptor[] interceptors) where TInterface : class;
    }
}
