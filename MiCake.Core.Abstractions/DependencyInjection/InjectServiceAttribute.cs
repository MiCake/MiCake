using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    /// <summary>
    /// Mark that the class is injected into the di framework
    /// 标记该类被注入到DI框架中
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InjectServiceAttribute : Attribute
    {
        public Type Type { get; set; }

        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;

        public bool TryRegister { get; set; }

        public bool ReplaceServices { get; set; }

        public InjectServiceAttribute(Type type)
        {
            Type = type;
        }
    }
}
