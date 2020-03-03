using System.Collections.Generic;
using System.Reflection;

namespace MiCake.DDD.Extensions.Metadata
{
    public interface IDomainMetadata
    {
        Assembly[] DomainLayerAssembly { get; }

        List<EntityDescriptor> Entities { get; }

        List<AggregateRootDescriptor> AggregateRoots { get; }
    }
}
