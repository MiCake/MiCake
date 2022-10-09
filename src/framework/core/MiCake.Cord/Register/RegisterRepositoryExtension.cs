using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Modularity
{
    public static class RegisterRepositoryExtension
    {
        /// <summary>
        /// Add customer repository.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serviceType">Interface type of repository</param>
        /// <param name="implementationType">Implementation type of repository</param>
        public static void RegisterRepository(this ModuleConfigServiceContext context, Type serviceType, Type implementationType)
        {
            if (!DomainTypeHelper.IsRepository(serviceType))
                throw new ArgumentException($"{serviceType.FullName} is not a {nameof(IRepository)},Please give a right type!");

            if (!DomainTypeHelper.IsRepository(implementationType))
                throw new ArgumentException($"{implementationType.FullName} is not a {nameof(IRepository)},Please give a right type!");

            var services = context.Services;
            services.AddTransient(serviceType, implementationType);
        }

        /// <summary>
        /// Add customer repository.
        /// </summary>
        /// <typeparam name="TService">Interface type of repository</typeparam>
        /// <typeparam name="TImpl">Implementation type of repository</typeparam>
        public static void RegisterRepository<TService, TImpl>(this ModuleConfigServiceContext context)
        {
            context.RegisterRepository(typeof(TService), typeof(TImpl));
        }
    }
}