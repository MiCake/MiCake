using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace MiCake.Core.Modularity
{
    public static class RegisterDomainServiceExtension
    {
        /// <summary>
        /// Add customer <see cref="IDomainService"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serviceType">Interface type of domain service</param>
        /// <param name="implementationType">ImplementationType type of domain service</param>
        /// <param name="miCakeServiceLifeTime"><see cref="MiCakeServiceLifetime"/></param>
        public static void RegisterDomainService(
            this ModuleConfigServiceContext context,
            Type serviceType,
            Type implementationType,
            MiCakeServiceLifetime miCakeServiceLifeTime = MiCakeServiceLifetime.Transient)
        {
            if (!DomainTypeHelper.IsDomainService(serviceType))
                throw new ArgumentException($"{serviceType.FullName} is not a domain service,Please give a right type!");

            if (!DomainTypeHelper.IsRepository(implementationType))
                throw new ArgumentException($"{implementationType.FullName} is not a domain service,Please give a right type!");

            var serviceDescpritor = new ServiceDescriptor(serviceType, implementationType, miCakeServiceLifeTime.ConvertToMSLifetime());
            context.Services.TryAdd(serviceDescpritor);
        }

        /// <summary>
        /// Add customer <see cref="IDomainService"/>
        /// </summary>
        /// <typeparam name="TService">Interface type of domain service</typeparam>
        /// <typeparam name="TImpl">ImplementationType type of domain service</typeparam>
        /// <param name="context"></param>
        /// <param name="miCakeServiceLifeTime"><see cref="MiCakeServiceLifetime"/></param>
        public static void RegisterDomainService<TService, TImpl>(
            this ModuleConfigServiceContext context,
            MiCakeServiceLifetime miCakeServiceLifeTime = MiCakeServiceLifetime.Transient)
        {
            RegisterDomainService(context, typeof(TService), typeof(TImpl), miCakeServiceLifeTime);
        }
    }
}
