using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Extensions.Register
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
