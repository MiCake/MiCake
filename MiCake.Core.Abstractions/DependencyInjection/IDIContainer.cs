using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    /// <summary>
    /// MiCake DI Container .  perform basic dependency injection operations or get actual Di framework container
    /// MiCake DI 容器。提供di的基础操作以及获取实际的di容器
    /// </summary>
    public interface IDIContainer
    {
        /// <summary>
        /// get actual Di framework container
        /// 获取实际的di框架容器
        /// </summary>
        /// <typeparam name="T">Container Type.</typeparam>
        T GetDIContainer<T>();

        /// <summary>
        /// injection a service in DI framework
        /// 向DI容器添加一个服务
        /// </summary>
        /// <param name="serviceType">The type of the service to register.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        /// <param name="serviceLifeTime"><see cref="MiCakeServiceLifeTime"/></param>
        IDIContainer AddService(Type serviceType, Type implementationType, MiCakeServiceLifeTime serviceLifeTime);

        /// <summary>
        /// injection a service in DI framework
        /// 向DI容器添加一个服务
        /// </summary>
        /// <typeparam name="TService">The type of the service to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type of the service.</typeparam>
        /// <param name="serviceLifeTime"><see cref="MiCakeServiceLifeTime"/></param>
        IDIContainer AddService<TService, TImplementation>(MiCakeServiceLifeTime serviceLifeTime) where TService : class where TImplementation : class, TService;

        /// <summary>
        /// injection a service in DI framework
        /// 向DI容器添加一个服务
        /// </summary>
        /// <param name="serviceType">The type of the service to register and the implementation to use.</typeparam>
        /// <param name="serviceLifeTime"><see cref="MiCakeServiceLifeTime"/></param>
        IDIContainer AddService(Type serviceType, MiCakeServiceLifeTime serviceLifeTime);

        /// <summary>
        /// injection a assembly service in DI framework
        /// </summary>
        IDIContainer AddAssembly(Assembly assembly);

        /// <summary>
        /// injection a assembly service in DI framework with filtter lambd expression
        /// </summary>
        IDIContainer AddAssembly(Assembly assembly, Func<Type, bool> predicate);

        /// <summary>
        /// get a service in DI farmework
        /// </summary>
        object GetService(Type serviceType);

        /// <summary>
        /// get a service in DI farmework
        /// </summary>
        object GetService<TService>() where TService : class;
    }
}
