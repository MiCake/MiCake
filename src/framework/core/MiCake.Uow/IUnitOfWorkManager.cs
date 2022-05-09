using MiCake.Uow.Internal;

namespace MiCake.Uow
{
    /// <summary>
    /// A <see cref="UnitOfWork"/> manager.
    /// Responsible for creating and managing work <see cref="UnitOfWork"/>
    /// </summary>
    public interface IUnitOfWorkManager : IDisposable
    {
        /// <summary>
        /// Get units of work in the current scope.
        /// </summary>
        IUnitOfWork? GetCurrentUnitOfWork();

        /// <summary>
        /// Create a <see cref="IUnitOfWork"/> with a default options.
        /// </summary>
        IUnitOfWork Create();
    }
}
