namespace MiCake.Cord.Storage
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Represents a description of an object that needs to be persisted.
    /// </summary>
    public interface IStoreEntityType
    {
        /// <summary>
        /// The name of this entity.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The Clr Type of this entity.
        /// </summary>
        Type ClrType { get; }
    }
}
