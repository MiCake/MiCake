namespace MiCake.Cord.Storage
{
    /// <summary>
    /// Contains all declared <see cref="IStoreEntityType"/> information
    /// </summary>
    public interface IStoreModel
    {
        /// <summary>
        ///  Adds an entity type to the model.
        /// </summary>
        /// <param name="clrType">The type of actual entity</param>
        IConventionStoreEntity AddStoreEntity(Type clrType);

        /// <summary>
        /// Find an entity type from the model.
        /// </summary>
        /// <param name="clrType">The type of actual entity</param>
        IConventionStoreEntity? FindStoreEntity(Type clrType);

        /// <summary>
        /// Remove an entity type from the model.
        /// </summary>
        /// <param name="clrType">The type of actual entity</param>
        void RemoveStoreEntity(Type clrType);

        /// <summary>
        /// Get All entites from the model.
        /// </summary>
        IEnumerable<IConventionStoreEntity> GetStoreEntities();
    }
}
