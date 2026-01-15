using System;
using System.Collections.Generic;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Delegate for finding service types to register automatically.
    /// When a class implements <see cref="ITransientService"/>, <see cref="ISingletonService"/>,
    /// or <see cref="IScopedService"/> interface, this delegate determines which types (interfaces) 
    /// should be registered for this class in the dependency injection container.
    /// </summary>
    /// <param name="type">The class type to register</param>
    /// <param name="inheritInterfaces">All interfaces inherited by this class</param>
    /// <returns>List of service types (interfaces) to register for this class</returns>
    public delegate List<Type> ServiceTypeDiscoveryHandler(Type type, List<Type> inheritInterfaces);
}
