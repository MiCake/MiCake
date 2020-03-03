using System.Collections.Generic;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Specifies the contract for a collection of <see cref="MiCakeModuleDescriptor"/>.
    /// </summary>
    public interface IMiCakeModuleCollection : IList<MiCakeModuleDescriptor>
    {
        //Get the assemblies of all micake modules
        Assembly[] GetAssemblies();

        //Get the assemblies of all micake modules
        Assembly[] GetAssemblies(bool includeFrameworkModule);
    }
}
