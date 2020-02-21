using System;
using System.Collections.Generic;

namespace MiCake.Core.DependencyInjection
{
    public interface IServiceLocator
    {
        /// <summary>
        /// Providing a "service locator"
        /// Please get the service through constructor or property injection first
        /// </summary>
        IServiceProvider Locator { get; set; }

        /// <summary>
        /// Get a service in ioc container
        /// </summary>
        /// <param name="type">service type</param>
        T GetSerivce<T>();

        /// <summary>
        /// Get services in ioc container
        /// </summary>
        /// <param name="type">service type</param>
        IEnumerable<T> GetSerivces<T>();
    }
}
