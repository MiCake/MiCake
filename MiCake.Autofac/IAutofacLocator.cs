using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Autofac
{
    public interface IAutofacLocator
    {
        /// <summary>
        /// Providing a "service locator"
        /// Please get the service through constructor or property injection first
        /// </summary>
        ILifetimeScope Locator { get; set; }

        /// <summary>
        /// Get a service in ioc container
        /// </summary>
        /// <param name="type">service type</param>
        T GetSerivce<T>();
    }
}
