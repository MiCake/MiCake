using MiCake.Core.Abstractions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// DefaultDIContainer Use Microsoft DI <see cref="IServiceCollection"/>
    /// </summary>
    public class DefaultDIContainer : BaseDIContainer
    {
        public DefaultDIContainer(IServiceCollection services) : base(services)
        {
        }

        public override IDIContainer AddAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                base.AddService(type, MiCakeServiceLifeTime.Transient);
            }
            return this;
        }

        public override IDIContainer AddAssembly(Assembly assembly, Func<Type, bool> predicate)
        {
            var types = assembly.GetTypes().Where(predicate).ToList();
            foreach (var type in types)
            {
                base.AddService(type, MiCakeServiceLifeTime.Transient);
            }
            return this;
        }
    }
}
