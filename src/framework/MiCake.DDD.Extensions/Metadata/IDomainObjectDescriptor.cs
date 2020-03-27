using System;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Describes an domain object.
    /// </summary>
    public interface IDomainObjectDescriptor
    {
        /// <summary>
        /// Object Type
        /// </summary>
        Type Type { get; }
    }

    /// <summary>
    /// Describes an domain object.
    /// </summary>
    public abstract class DomainObjectDescriptor : IDomainObjectDescriptor
    {
        public virtual Type Type { get; private set; }

        public DomainObjectDescriptor(Type type)
        {
            Type = type;
        }
    }
}
