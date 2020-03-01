using MiCake.Core.Builder;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using Microsoft.Extensions.DependencyInjection;
using System;

// Default method,so namespace is the highest level
namespace MiCake
{
    public static class RegisterRepositoryForBuilderExtension
    {
        /// <summary>
        /// Add customer repository.
        /// </summary>
        /// <param name="serviceType">Interface type of repository</param>
        /// <param name="implementationType">Implementation type of repository</param>
        public static void RegisterRepository(this IMiCakeBuilder builder, Type serviceType, Type implementationType)
        {
            if (!DomianTypeHelper.IsRepository(serviceType))
                throw new ArgumentException($"{serviceType.FullName} is not a {nameof(IRepository)},Please give a right type!");

            if (!DomianTypeHelper.IsRepository(implementationType))
                throw new ArgumentException($"{implementationType.FullName} is not a {nameof(IRepository)},Please give a right type!");

            var services = builder.Services;

            services.AddTransient(serviceType, implementationType);
        }

        /// <summary>
        /// Add customer repository.
        /// </summary>
        /// <typeparam name="TService">Interface type of repository</typeparam>
        /// <typeparam name="TImpl">Implementation type of repository</typeparam>
        public static void RegisterRepository<TService, TImpl>(this IMiCakeBuilder builder)
        {
            RegisterRepository(builder, typeof(TService), typeof(TImpl));
        }
    }
}