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
        public virtual Type Type { get; set; }

        public virtual MiCakeServiceLifeTime? Lifetime { get; set; }

        public virtual bool TryRegister { get; set; }

        public virtual bool ReplaceServices { get; set; }

        public InjectServiceAttribute()
        {
        }

        public InjectServiceAttribute(Type type, MiCakeServiceLifeTime lifeTime)
        {
            Type = type;
            Lifetime = lifeTime;
        }
    }
}
