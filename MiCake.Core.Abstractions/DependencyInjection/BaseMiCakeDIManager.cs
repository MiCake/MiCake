using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    public abstract class BaseMiCakeDIManager : IMiCakeDIManager
    {
        protected IServiceCollection _services;

        public BaseMiCakeDIManager(IServiceCollection services)
        {
            _services = services;
        }

        public virtual void PopulateAutoService(IMiCakeModuleCollection miCakeModules)
        {
            throw new NotImplementedException();
        }

        //根据type继承的接口类型返回服务生命周期
        protected virtual MiCakeServiceLifeTime? GetServiceLifetime(Type type)
        {
            if (typeof(ITransientService).GetTypeInfo().IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Transient;
            }

            if (typeof(ISingletonService).GetTypeInfo().IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Singleton;
            }

            if (typeof(IScopedService).GetTypeInfo().IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Scoped;
            }

            return null;
        }
    }
}
