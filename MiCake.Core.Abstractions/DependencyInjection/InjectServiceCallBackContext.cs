using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{

    public struct InjectServiceCallBackContext
    {
        public Type Type { get; set; }

        public Type ImplementationType { get; set; }

        public MiCakeServiceLifeTime? Lifetime { get; set; }

        public InjectServiceCallBackContext(Type type, Type impementationType, MiCakeServiceLifeTime? lifetime)
        {
            Type = type;
            ImplementationType = impementationType;
            Lifetime = lifetime;
        }
    }
}
