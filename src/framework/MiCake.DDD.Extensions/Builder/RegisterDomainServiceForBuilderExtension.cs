using MiCake.Core;
using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Default method,so namespace is the highest level
namespace MiCake
{
    public static class RegisterDomainServiceForBuilderExtension
    {
        /// <summary>
        /// Add customer <see cref="IDomainService"/>
        /// </summary>
        /// <param name="serviceType">Interface type of domain service</param>
        /// <param name="implementationType">ImplementationType type of domain service</param>
        /// <param name="miCakeServiceLifeTime"><see cref="MiCakeServiceLifetime"/></param>
        public static void RegisterDomainService(
            this IMiCakeBuilder builder,
            Type serviceType,
            Type implementationType,
            MiCakeServiceLifetime miCakeServiceLifeTime = MiCakeServiceLifetime.Transient)
        {
            if (!DomianTypeHelper.IsDomainService(serviceType))
                throw new ArgumentException($"{serviceType.FullName} is not a domain service,Please give a right type!");

            if (!DomianTypeHelper.IsRepository(implementationType))
                throw new ArgumentException($"{implementationType.FullName} is not a domain service,Please give a right type!");

            var serviceDescpritor = new ServiceDescriptor(serviceType, implementationType, miCakeServiceLifeTime.ConvertToMSLifetime());
            builder.Services.TryAdd(serviceDescpritor);
        }

        /// <summary>
        /// Add customer <see cref="IDomainService"/>
        /// </summary>
        /// <typeparam name="TService">Interface type of domain service</typeparam>
        /// <typeparam name="TImpl">ImplementationType type of domain service</typeparam>
        /// <param name="miCakeServiceLifeTime"><see cref="MiCakeServiceLifetime"/></param>
        public static void RegisterDomainService<TService, TImpl>(
            this IMiCakeBuilder builder,
            MiCakeServiceLifetime miCakeServiceLifeTime = MiCakeServiceLifetime.Transient)
        {
            RegisterDomainService(builder, typeof(TService), typeof(TImpl), miCakeServiceLifeTime);
        }

    }
}
