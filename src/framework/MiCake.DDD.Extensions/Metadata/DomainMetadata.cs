using System.Collections.Generic;
using System.Reflection;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Object metadata in domain layer
    /// </summary>
    public class DomainMetadata : IDomainMetadata
    {
        public DomainMetadata()
        {
        }

        public List<EntityDescriptor> Entities { get; set; }

        public List<AggregateRootDescriptor> AggregateRoots { get; set; }

        public Assembly[] DomainLayerAssembly { get; set; }
    }
}
