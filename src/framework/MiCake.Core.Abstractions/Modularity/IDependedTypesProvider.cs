using System;

namespace MiCake.Core.Modularity
{
    internal interface IDependedTypesProvider
    {
        Type[] GetDependedTypes();
    }
}
