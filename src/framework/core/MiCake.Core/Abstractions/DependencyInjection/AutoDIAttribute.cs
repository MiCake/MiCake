using MiCake.Core.Modularity;
using System;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Tag current <see cref="MiCakeModule"/> need auto register services to DI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoDIAttribute : Attribute
    {
    }
}
