using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace MiCake.Core.DependencyInjection
{
    public class ServiceLocator : IServiceLocator
    {
        public static ServiceLocator Instance { get; private set; }

        /// <summary>
        /// Providing a "service locator"
        /// [The "service locator" is not recommended if you can try constructor injection]
        /// </summary>
        public IServiceProvider Locator { get; set; }

        static ServiceLocator()
        {
            Instance = new ServiceLocator();
        }

        /// <summary>
        /// Get a service in ioc container
        /// [The "service locator" is not recommended if you can try constructor injection]
        /// </summary>
        /// <param name="type">service type</param>
        public T GetSerivce<T>()
        {
            if (Locator == null)
                throw new ArgumentException("the Locator is null.Please check if you are calling after the Startup.cs Configure method. ");

            return (T)Locator.GetRequiredService(typeof(T));
        }

        /// <summary>
        /// Get services in ioc container
        /// [The "service locator" is not recommended if you can try constructor injection]
        /// </summary>
        /// <param name="type">service type</param>
        public IEnumerable<T> GetSerivces<T>()
        {
            if (Locator == null)
                throw new ArgumentException("the Locator is null.Please check if you are calling after the Startup.cs Configure method. ");

            return (IEnumerable<T>)Locator.GetServices(typeof(T));
        }
    }
}
