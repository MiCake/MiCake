using System.Reflection;

namespace MiCake.Cord.Storage
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Represents a description of an object property that needs to be persisted.
    /// </summary>
    public interface IStoreProperty
    {
        /// <summary>
        /// The name of property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The clr <see cref="PropertyInfo"/> of this property.
        /// 
        /// This parameter may be empty due to the relationship between fields and properties.
        /// </summary>
        PropertyInfo ClrPropertyInfo { get; }

        /// <summary>
        /// The <see cref="IStoreEntityType"/> depends on.
        /// </summary>
        IStoreEntityType StoreEntityType { get; }
    }
}
