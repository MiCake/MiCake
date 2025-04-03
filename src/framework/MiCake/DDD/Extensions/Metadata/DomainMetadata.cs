using System.Reflection;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// A metadata for micake domain layer.
    /// </summary>
    public class DomainMetadata
    {
        /// <summary>
        /// Assembly containing domain objects
        /// </summary>
        public Assembly[] DomainLayerAssembly { get; }

        /// <summary>
        /// Model for all domain object descriptor.
        /// </summary>
        public DomainObjectModel DomainObject { get; }

        public DomainMetadata(Assembly[] assemblies, DomainObjectModel domainObjectModel)
        {
            DomainLayerAssembly = assemblies;

            DomainObject = domainObjectModel;
        }
    }
}
