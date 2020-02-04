using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    /// <summary>
    /// Specifies the lifetime of a service
    /// </summary>
    public enum MiCakeServiceLifeTime
    {
        Singleton = 0,
        Scoped = 1,
        Transient = 2
    }
}
