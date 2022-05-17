namespace MiCake.Cord.Lifetime
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// </summary>
    public interface IRepositoryPostSaveChanges : IRepositoryLifetime
    {
        /// <summary>
        /// Operations after domain object persistence
        /// </summary>
        ValueTask<RepositoryEntityState> PostSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default);
    }
}
