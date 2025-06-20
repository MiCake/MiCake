﻿using System.Collections.Generic;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// A model for interpreting domain objects
    /// </summary>
    public class DomainObjectModel
    {
        /// <summary>
        /// Entity objects defined in the system
        /// <see cref="EntityDescriptor"/>
        /// </summary>
        public List<EntityDescriptor> Entities { get; } = [];

        /// <summary>
        /// AggregateRoot objects defined in the system
        /// <see cref="AggregateRootDescriptor"/>
        /// </summary>
        public List<AggregateRootDescriptor> AggregateRoots { get; } = [];

        /// <summary>
        /// Value objects defined in the system
        /// </summary>
        public List<VauleObjectDescriptor> VauleObjects { get; } = [];
    }
}
