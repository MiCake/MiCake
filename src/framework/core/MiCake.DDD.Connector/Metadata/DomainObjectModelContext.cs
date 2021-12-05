using System.Reflection;

namespace MiCake.DDD.Connector.Metadata
{
    /// <summary>
    ///  A context object for <see cref="IDomainObjectModelProvider"/>.
    /// </summary>
    public class DomainObjectModelContext
    {
        /// <summary>
        /// Assembly containing domain objects
        /// </summary>
        public Assembly[] DomainLayerAssembly { get; }

        /// <summary>
        /// A result of <see cref="DomainObjectModel"/>
        /// </summary>
        public DomainObjectModel Result { get; } = new DomainObjectModel();

        public DomainObjectModelContext(Assembly[] assemblies)
        {
            DomainLayerAssembly = assemblies;
        }
    }
}
