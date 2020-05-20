namespace MiCake.Uow
{
    /// <summary>
    /// A <see cref="UnitOfWork"/> manager.
    /// Responsible for creating and managing work <see cref="UnitOfWork"/>
    /// </summary>
    public interface IUnitOfWorkManager : IUnitOfWorkProvider
    {
        /// <summary>
        /// Create a <see cref="IUnitOfWork"/> with a default options
        /// </summary>
        IUnitOfWork Create();

        /// <summary>
        ///  Create a <see cref="IUnitOfWork"/> with a custom options
        /// </summary>
        /// <param name="options"><see cref="UnitOfWorkOptions"/></param>
        IUnitOfWork Create(UnitOfWorkOptions options);
    }
}
