using System.Reflection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Specifies the contract for a collection of <see cref="MiCakeModuleDescriptor"/>.
    /// </summary>
    public interface IMiCakeModuleCollection : IList<MiCakeModuleDescriptor>
    {
        /// <summary>
        /// Get the assemblies of all micake modules
        /// </summary>
        /// <param name="includeFrameworkModule">is include framework module</param>
        /// <returns>All assemblies containing <see cref="MiCakeModule"/></returns>
        Assembly[] GetAssemblies(bool includeFrameworkModule = false);
    }
}
