namespace MiCake.Uow
{
    /// <summary>
    /// Defined a child <see cref="IUnitOfWork"/>.
    /// It will use its parent's configuration
    /// </summary>
    public interface IChildUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Get the parent of current <see cref="ICurrentUnitOfWork"/>.
        /// </summary>
        IUnitOfWork GetParentUnitOfWork();
    }
}
