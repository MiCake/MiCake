using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    /// <summary>
    /// Specifies the contract for a collection of <see cref="MiCakeModuleDescriptor"/>.
    /// </summary>
    public interface IMiCakeModuleCollection:IList<MiCakeModuleDescriptor>
    {
    }
}
