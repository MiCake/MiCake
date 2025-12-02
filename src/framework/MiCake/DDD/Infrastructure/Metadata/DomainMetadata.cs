using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.DDD.Infrastructure.Metadata
{
    /// <summary>
    /// Simplified domain metadata containing information about domain objects.
    /// Provides fast lookup and discovery of entities, aggregates, and value objects.
    /// </summary>
    public class DomainMetadata
    {
        private readonly Dictionary<Type, DomainTypeDescriptor> _descriptorCache = [];
        
        /// <summary>
        /// Assemblies containing domain objects
        /// </summary>
        public IReadOnlyCollection<Assembly> Assemblies { get; }
        
        /// <summary>
        /// All aggregate root descriptors
        /// </summary>
        public IReadOnlyCollection<AggregateRootDescriptor> AggregateRoots { get; }
        
        /// <summary>
        /// All entity descriptors (includes aggregate roots)
        /// </summary>
        public IReadOnlyCollection<EntityDescriptor> Entities { get; }
        
        /// <summary>
        /// All value object descriptors
        /// </summary>
        public IReadOnlyCollection<ValueObjectDescriptor> ValueObjects { get; }

        internal DomainMetadata(
            IEnumerable<Assembly> assemblies,
            IEnumerable<AggregateRootDescriptor> aggregates,
            IEnumerable<EntityDescriptor> entities,
            IEnumerable<ValueObjectDescriptor> valueObjects)
        {
            Assemblies = assemblies.ToList().AsReadOnly();
            AggregateRoots = aggregates.ToList().AsReadOnly();
            Entities = entities.ToList().AsReadOnly();
            ValueObjects = valueObjects.ToList().AsReadOnly();
            
            // Build cache for fast lookup
            foreach (var descriptor in AggregateRoots)
                _descriptorCache[descriptor.Type] = descriptor;
            foreach (var descriptor in Entities)
                _descriptorCache.TryAdd(descriptor.Type, descriptor);
            foreach (var descriptor in ValueObjects)
                _descriptorCache[descriptor.Type] = descriptor;
        }

        /// <summary>
        /// Gets the descriptor for a specific domain object type
        /// </summary>
        public DomainTypeDescriptor? GetDescriptor(Type type)
        {
            return _descriptorCache.TryGetValue(type, out var descriptor) ? descriptor : null;
        }

        /// <summary>
        /// Gets the descriptor for a specific domain object type
        /// </summary>
        public TDescriptor? GetDescriptor<TDescriptor>(Type type) where TDescriptor : DomainTypeDescriptor
        {
            return GetDescriptor(type) as TDescriptor;
        }

        /// <summary>
        /// Checks if the type is a registered domain object
        /// </summary>
        public bool IsDomainObject(Type type)
        {
            return _descriptorCache.ContainsKey(type);
        }
    }
}
