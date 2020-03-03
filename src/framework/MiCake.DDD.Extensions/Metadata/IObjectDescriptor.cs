using System;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Describes an domain object.
    /// </summary>
    public interface IObjectDescriptor
    {
        /// <summary>
        /// Object Type
        /// </summary>
        Type Type { get; }
    }

    /// <summary>
    /// Describes an domain object.
    /// </summary>
    public abstract class ObjectDescriptor : IObjectDescriptor
    {
        public virtual Type Type { get; private set; }

        public ObjectDescriptor(Type type)
        {
            Type = type;
        }
    }
}
