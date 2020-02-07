using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    internal interface IDependedTypesProvider
    {
        Type[] GetDependedTypes();
    }
}
