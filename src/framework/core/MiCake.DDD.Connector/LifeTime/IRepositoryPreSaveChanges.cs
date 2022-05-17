namespace MiCake.Cord.Lifetime
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// </summary>
    public interface IRepositoryPreSaveChanges : IRepositoryLifetime
    {
        /// <summary>
        /// Operations before domain object persistence
        /// </summary>
        ValueTask<RepositoryEntityState> PreSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default);
    }
}
