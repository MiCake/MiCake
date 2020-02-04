using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Extensions
{
    /// <summary>
    /// this interface provider register the repository into the dependency injection framework.
    /// </summary>
    public interface IRepositoryRegister
    {
        /// <summary>
        /// register the find repository into di framework
        /// </summary>
        /// <param name="miCakeModules"><see cref="IMiCakeModuleCollection"/></param>
        /// <param name="services"><see cref=" IServiceCollection"/></param>
        void Register(IMiCakeModuleCollection miCakeModules, IServiceCollection services);
    }
}
