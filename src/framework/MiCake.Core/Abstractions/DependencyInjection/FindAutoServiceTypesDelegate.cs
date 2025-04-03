using System;
using System.Collections.Generic;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// When a class that implements an <see cref="ITransientService"/> or <see cref="ISingletonService"/>
    /// or <see cref="IScopedService"/> interface will be injected automatically.
    /// But we need to determine which type of service this class is.
    /// </summary>
    /// <param name="type">the class type</param>
    /// <param name="inheritInterfaces">All interfaces inherited by this class</param>
    /// <returns>service types</returns>
    public delegate List<Type> FindAutoServiceTypesDelegate(Type type, List<Type> inheritInterfaces);
}
