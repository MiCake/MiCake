using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    public abstract class BaseDIContainer : IDIContainer
    {
        private IServiceCollection _services;

        public BaseDIContainer(IServiceCollection services)
        {
            _services = services;
        }

        public virtual IDIContainer AddAssembly(Assembly assembly)
        {
            throw new NotImplementedException();
        }

        public virtual IDIContainer AddAssembly(Assembly assembly, Func<Type, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public virtual IDIContainer AddService(Type serviceType, Type implementationType, MiCakeServiceLifeTime serviceLifeTime)
        {
            switch (serviceLifeTime)
            {
                case MiCakeServiceLifeTime.Singleton:
                    _services.AddSingleton(serviceType, implementationType);
                    break;
                case MiCakeServiceLifeTime.Scoped:
                    _services.AddScoped(serviceType, implementationType);
                    break;
                case MiCakeServiceLifeTime.Transient:
                    _services.AddTransient(serviceType, implementationType);
                    break;
                default:
                    break;
            }
            return this;
        }

        public virtual IDIContainer AddService(Type serviceType, MiCakeServiceLifeTime serviceLifeTime)
        {
            switch (serviceLifeTime)
            {
                case MiCakeServiceLifeTime.Singleton:
                    _services.AddSingleton(serviceType);
                    break;
                case MiCakeServiceLifeTime.Scoped:
                    _services.AddScoped(serviceType);
                    break;
                case MiCakeServiceLifeTime.Transient:
                    _services.AddTransient(serviceType);
                    break;
                default:
                    break;
            }
            return this;
        }

        public virtual IDIContainer AddService<TService, TImplementation>(MiCakeServiceLifeTime serviceLifeTime) where TService : class where TImplementation : class, TService
        {
            switch (serviceLifeTime)
            {
                case MiCakeServiceLifeTime.Singleton:
                    _services.AddSingleton<TService, TImplementation>();
                    break;
                case MiCakeServiceLifeTime.Scoped:
                    _services.AddScoped<TService, TImplementation>();
                    break;
                case MiCakeServiceLifeTime.Transient:
                    _services.AddTransient<TService, TImplementation>();
                    break;
                default:
                    break;
            }
            return this;
        }

        public virtual T GetDIContainer<T>()
        {
            if (!(typeof(T) is IServiceCollection))
                throw new InvalidCastException($"Cannot Cast Current DI Container to {typeof(T).Name} . Because of Current DI Container type is IServiceCollection");

            return (T)_services;
        }

        public virtual object GetService(Type serviceType)
        {
            return _services.BuildServiceProvider().GetService(serviceType);
        }

        public virtual object GetService<TService>() where TService : class
        {
            return _services.BuildServiceProvider().GetService<TService>();
        }
    }
}
