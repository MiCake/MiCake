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

        public List<EntityDescriptor> Entities { get; set; } = new List<EntityDescriptor>();

        public List<AggregateRootDescriptor> AggregateRoots { get; set; } = new List<AggregateRootDescriptor>();

        public Assembly[] DomainLayerAssembly { get; set; }
    }
}
